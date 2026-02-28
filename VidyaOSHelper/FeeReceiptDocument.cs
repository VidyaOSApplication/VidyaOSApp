using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VidyaOSDAL.DTOs;
using System.Globalization;

namespace VidyaOSHelper
{
    public class FeeReceiptDocument : IDocument
    {
        // 🚀 Matches your FeeReceiptDto exactly
        public FeeReceiptDto Model { get; }
        private const string SuccessGreen = "#10B981";
        private const string LightGreenBG = "#F0FDF4";

        public FeeReceiptDocument(FeeReceiptDto model) => Model = model;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(35);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                // 1. Header Section
                page.Header().Row(row => {
                    row.RelativeItem().Column(col => {
                        col.Item().Text(Model.SchoolName?.ToUpper() ?? "SCHOOL NAME").FontSize(20).ExtraBold().FontColor(SuccessGreen);
                        col.Item().Text(Model.SchoolAddress ?? "").FontSize(9);
                    });
                    row.ConstantItem(120).Column(c => {
                        c.Item().Background(SuccessGreen).Padding(5).AlignCenter().Text("FEE RECEIPT").FontColor(Colors.White).Bold().FontSize(9);
                        // 🚀 Uses GenerationDateTime for the display date
                        c.Item().PaddingTop(5).AlignCenter().Text(Model.GenerationDateTime.ToString("dd MMM yyyy")).FontSize(9).SemiBold();
                        c.Item().AlignCenter().Text(Model.GenerationDateTime.ToString("hh:mm tt IST")).FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });

                // 2. Content Section
                page.Content().PaddingVertical(20).Column(col =>
                {
                    // Receipt Details Bar - Handles Session and Receipt No
                    col.Item().Background(LightGreenBG).Padding(10).Row(row => {
                        row.RelativeItem().Text(t => { t.Span("Receipt No: ").SemiBold(); t.Span(Model.ReceiptNo ?? "N/A"); });
                        row.RelativeItem().AlignRight().Text(t => { t.Span("Session: ").SemiBold(); t.Span(Model.AcademicYear ?? "N/A"); });
                    });

                    // Student Details Grid
                    col.Item().PaddingTop(20).Text("STUDENT DETAILS").FontSize(10).SemiBold().FontColor(SuccessGreen);
                    col.Item().Table(table => {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });

                        table.Cell().Element(LabelStyle).Text("Student Name");
                        table.Cell().ColumnSpan(3).Element(ValueStyle).Text(Model.StudentName);

                        table.Cell().Element(LabelStyle).Text("Admission No");
                        table.Cell().Element(ValueStyle).Text(Model.AdmissionNo);

                        table.Cell().Element(LabelStyle).Text("Class & Sec");
                        table.Cell().Element(ValueStyle).Text($"{Model.ClassName} - {Model.SectionName}");

                        table.Cell().Element(LabelStyle).Text("Roll No");
                        table.Cell().Element(ValueStyle).Text(Model.RollNo);

                        table.Cell().Element(LabelStyle).Text("Mode");
                        table.Cell().Element(ValueStyle).Text(Model.PaymentMode);
                    });

                    // Payment Table
                    col.Item().PaddingTop(30).Table(table => {
                        table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(100); });
                        table.Header(h => {
                            h.Cell().Background(LightGreenBG).Padding(8).Text("Description").Bold();
                            h.Cell().Background(LightGreenBG).Padding(8).AlignRight().Text("Amount").Bold();
                        });
                        table.Cell().Padding(8).Text(t => {
                            t.Span("Monthly School Fee").SemiBold();
                            // 🚀 Formats "2026-6" to "June 2026"
                            t.Span($" for {FormatMonth(Model.FeeMonth)}").FontColor(Colors.Grey.Medium);
                        });
                        table.Cell().Padding(8).AlignRight().Text($"₹{Model.TotalAmount:N2}");
                    });

                    // Amount in Words & Total
                    col.Item().PaddingTop(20).Row(row => {
                        row.RelativeItem().Column(c => {
                            c.Item().Text("Amount in Words:").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{NumberToWordsIndian((long)Model.TotalAmount)} Rupees Only").FontSize(9).Italic();
                        });
                        row.ConstantItem(150).Background(SuccessGreen).Padding(10).Text($"PAID: ₹{Model.TotalAmount:N2}").FontColor(Colors.White).Bold().AlignCenter();
                    });
                });

                page.Footer().AlignCenter().Text("Powered by VidyaOS™ Cloud").FontSize(8).Italic();
            });
        }

        private string FormatMonth(string feeMonth)
        {
            if (DateTime.TryParseExact(feeMonth, "yyyy-M", null, DateTimeStyles.None, out DateTime d))
                return d.ToString("MMMM yyyy");
            return feeMonth;
        }

        private string NumberToWordsIndian(long number)
        {
            if (number == 0) return "Zero";
            string words = "";
            if (number >= 10000000) { words += NumberToWordsIndian(number / 10000000) + " Crore "; number %= 10000000; }
            if (number >= 100000) { words += NumberToWordsIndian(number / 100000) + " Lakh "; number %= 100000; }
            if (number >= 1000) { words += NumberToWordsIndian(number / 1000) + " Thousand "; number %= 1000; }
            if (number >= 100) { words += NumberToWordsIndian(number / 100) + " Hundred "; number %= 100; }
            if (number > 0)
            {
                if (words != "") words += "and ";
                var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
                if (number < 20) words += unitsMap[number];
                else { words += tensMap[number / 10]; if ((number % 10) > 0) words += "-" + unitsMap[number % 10]; }
            }
            return words.Trim();
        }

        static IContainer LabelStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));
        static IContainer ValueStyle(IContainer container) => container.PaddingVertical(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).DefaultTextStyle(x => x.FontSize(10).SemiBold());
    }
}