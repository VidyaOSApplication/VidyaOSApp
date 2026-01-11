
using BCrypt.Net;

namespace VidyaOSConsoleApp
{

    class Program
    {
        static void Main()
        {
            string plainPassword = "Dhruv@1515";

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            Console.WriteLine(hashedPassword);
        }
    }

}
