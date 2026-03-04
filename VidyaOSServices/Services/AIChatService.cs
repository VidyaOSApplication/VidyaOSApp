using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VidyaOSDAL.DTOs;

namespace VidyaOSServices.Services
{
    public class AIChatService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AIChatService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        private string GetFullSchemaContext(int schoolId)
        {
            return $@"
            You are the Vidya AI. Use this exact schema for SchoolId {schoolId}.

            CRITICAL SECURITY RULE:
            - You are ONLY allowed to see data for SchoolId = {schoolId}.
            - Every single query MUST contain 'Where SchoolId = {schoolId}'.
            - If you fail to include the SchoolId filter, the system will block the request.
            
            TABLES:
            - Students (StudentId, UserId, SchoolId, FirstName, LastName, Gender, DOB, ClassId, SectionId, RollNo, StreamId, Category)
            - Teachers (TeacherId, SchoolId, UserId, FullName, Qualification, JoiningDate)
            - Users (UserId, SchoolId, Username, Role ['Student','Teacher','SchoolAdmin'], IsActive)
            - Attendance (AttendanceId, SchoolId, UserId, AttendanceDate, Status ['Present','Absent'])
            - StudentFees (StudentFeeId, StudentId, SchoolId, Amount, Status ['Paid','Pending'], FeeMonth ['yyyy-M'], PaidOn)
            - StudentMarks (StudentMarkId, SchoolId, StudentId, ExamId, ClassId, SubjectId, MarksObtained, MaxMarks)
            - Exams (ExamId, SchoolId, ExamName, AcademicYear, StartDate, Status)
            - Classes (SchoolId, ClassId, ClassName)
            - Sections (SchoolId, ClassId, SectionId, SectionName)
            - ClassTimetables (TimetableId, SchoolId, ClassId, SectionId, SubjectId, DayOfWeek [1=Sun, 2=Mon...], StartTime, EndTime)
            - Subjects (SubjectId, SchoolId, ClassId, SubjectName)

            JOIN RULES:
            1. Students to Attendance: JOIN Attendance a ON s.UserId = a.UserId
            2. Students to Marks/Fees: JOIN StudentMarks/StudentFees ON s.StudentId = m.StudentId
            3. FeeMonth Format: Use '2026-1' for Jan, '2026-2' for Feb (NO leading zeros).
            4. Stream Filtering: NEVER compare 'PCM' or 'Arts' to StreamId. 
             ALWAYS use: JOIN Streams st ON s.StreamId = st.StreamId WHERE st.StreamName = 'PCM' and schoolId = {schoolId}

            GOLDEN SQL PATTERNS:
            - Absent on Date: SELECT s.FirstName FROM Students s JOIN Attendance a ON s.UserId = a.UserId WHERE a.Status = 'Absent' AND a.AttendanceDate = '2026-03-01' AND s.SchoolId = {schoolId}
            - Pending Fees: SELECT s.FirstName, f.Amount FROM Students s JOIN StudentFees f ON s.StudentId = f.StudentId WHERE f.Status = 'Pending' AND f.FeeMonth = '2026-4' AND s.SchoolId = {schoolId}
            - Exam Toppers: SELECT TOP 1 s.FirstName, m.MarksObtained FROM StudentMarks m JOIN Students s ON m.StudentId = s.StudentId JOIN Exams e ON m.ExamId = e.ExamId WHERE e.ExamName = 'Final' AND s.SchoolId = {schoolId} ORDER BY m.MarksObtained DESC
            - Stream Students: SELECT s.FirstName, s.LastName FROM Students s JOIN Streams st ON s.StreamId = st.StreamId WHERE st.StreamName = 'PCM' AND s.ClassId = 12 AND s.SchoolId = {schoolId}
            Return ONLY raw SQL. No markdown. If question is not school-related, reply 'OFF_TOPIC'.";
        }

        public async Task<string> AskAsync(int schoolId, string question)
        {
            try
            {
                // 1. SQL Generation
                string sqlQuery = await CallGroqAsync(GetFullSchemaContext(schoolId), $"Admin Question: {question}", isSqlTask: true);

                if (sqlQuery.Contains("OFF_TOPIC")) return "Bhaiya/Behen, please ask questions related to school data only.";

                sqlQuery = CleanSql(sqlQuery);
                if (IsDangerousSQL(sqlQuery)) return "Security Block: This operation is not allowed.";

                // 2. Data Retrieval
                string jsonData;
                try
                {
                    jsonData = await ExecuteSQLAsync(sqlQuery);
                }
                catch (SqlException ex)
                {
                    // Automatic Correction Logic
                    string fixPrompt = $"{GetFullSchemaContext(schoolId)}\n\nSQL Error: {ex.Message}. Fix this SQL: {sqlQuery}";
                    sqlQuery = await CallGroqAsync(fixPrompt, "Fixed SQL:", isSqlTask: true);
                    jsonData = await ExecuteSQLAsync(sqlQuery);
                }

                // 3. Humanized Result
                string humanPrompt = $@"
                    User Question: {question}
                    SQL Result Data: {jsonData}
                    SQL Query Used: {sqlQuery}
                    
                    Instructions: 
                    - Reply like a helpful school secretary.
                    - NEVER mention words like 'SQL', 'Query', 'Database', or 'Table'.
                    - Do not say 'Based on the results'.
                    - Just give the direct answer. 
                    - Example: Instead of 'Based on SQL, there are 5 students', say 'There are 5 students in Class 10.';
                    - Today's date is {DateTime.Now:yyyy-MM-dd}.";

                return await CallGroqAsync("You are the VidyaOS Assistant.", humanPrompt, isSqlTask: false);
            }
            catch (Exception ex)
            {
                return $"Service Error: {ex.Message}";
            }
        }

        private async Task<string> CallGroqAsync(string systemContent, string userContent, bool isSqlTask)
        {
            var apiKey = _config["Groq:ApiKey"];
            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[] {
                    new { role = "system", content = systemContent },
                    new { role = "user", content = userContent }
                },
                temperature = isSqlTask ? 0.0 : 0.4
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }

        private async Task<string> ExecuteSQLAsync(string sql)
        {
            var connectionString = _config.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                results.Add(row);
            }
            return results.Count == 0 ? "[]" : JsonSerializer.Serialize(results);
        }

        private string CleanSql(string sql) => sql.Replace("```sql", "").Replace("```", "").Trim();

        private bool IsDangerousSQL(string sql)
        {
            string[] forbidden = { "DROP", "DELETE", "UPDATE", "INSERT", "TRUNCATE", "ALTER" };
            return forbidden.Any(k => sql.ToUpper().Contains(k));
        }
    }
}