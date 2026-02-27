using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VidyaOSDAL.DTOs;

namespace VidyaOSHelper
{
    public class FeeReceiptDocument : IDocument
    {
        public FeeReceiptDto Model { get; }
        private const string SuccessGreen = "#10B981"; // 🚀 Success Theme
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
                page.Header().Element(ComposeHeader);

                // 2. Content Section with Background Watermark
                page.Content().Layers(layers =>
                {
                    // Layer 1: Background Watermark
                    layers.Layer().AlignCenter().AlignMiddle().Rotate(-30).Text("PAID")
                        .FontSize(120)
                        .ExtraBold()
                        .FontColor("#E1F2E9"); // Very light green

                    // Layer 2: Main Content
                    layers.PrimaryLayer().PaddingVertical(20).Column(col =>
                    {
                        // Receipt Summary Bar
                        col.Item().Background(LightGreenBG).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text(t => { t.Span("Receipt No: ").SemiBold(); t.Span(Model.ReceiptNo); });
                            row.RelativeItem().AlignRight().Text(t => { t.Span("Academic Session: ").SemiBold(); t.Span(Model.AcademicYear ?? ""); });
                        });

                        // Student Information Grid
                        col.Item().PaddingTop(25).Text("STUDENT DETAILS").FontSize(11).SemiBold().FontColor(SuccessGreen);
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns => {
                                columns.RelativeColumn(); columns.RelativeColumn();
                                columns.RelativeColumn(); columns.RelativeColumn();
                            });

                            table.Cell().Element(LabelStyle).Text("Student Name");
                            table.Cell().ColumnSpan(3).Element(ValueStyle).Text(Model.StudentName);

                            table.Cell().Element(LabelStyle).Text("Admission No");
                            table.Cell().Element(ValueStyle).Text(Model.AdmissionNo);

                            table.Cell().Element(LabelStyle).Text("Class & Sec");
                            table.Cell().Element(ValueStyle).Text($"{Model.ClassName} - {Model.SectionName}");

                            table.Cell().Element(LabelStyle).Text("Roll Number");
                            table.Cell().Element(ValueStyle).Text(Model.RollNo ?? "N/A");

                            table.Cell().Element(LabelStyle).Text("Payment Mode");
                            table.Cell().Element(ValueStyle).Text(Model.PaymentMode);
                        });

                        // Payment Particulars
                        col.Item().PaddingTop(30).Text("PAYMENT PARTICULARS").FontSize(11).SemiBold().FontColor(SuccessGreen);
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(columns => {
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                            });

                            table.Header(header => {
                                header.Cell().Element(HeaderStyle).Text("Description");
                                header.Cell().Element(HeaderStyle).AlignRight().Text("Amount (INR)");
                            });

                            table.Cell().Element(CellStyle).Text(text => {
                                text.Span("Monthly Schhol Fee - ").SemiBold();
                                text.Span($" for {Model.FeeMonth}").FontColor(Colors.Grey.Medium); // 🚀 Displays: "for April 2026"
                            });
                            table.Cell().Element(CellStyle).AlignRight().Text($"{Model.TotalAmount:N2}");
                        });

                        // Paid Amount Box
                        col.Item().AlignRight().PaddingTop(15).Row(row => {
                            row.ConstantItem(170).Background(SuccessGreen).Padding(12).Row(r => {
                                r.RelativeItem().Text("PAID AMOUNT").FontColor(Colors.White).Bold().FontSize(11);
                                r.RelativeItem().AlignRight().Text($"₹{Model.TotalAmount:N2}").FontColor(Colors.White).Bold().FontSize(11);
                            });
                        });
                    });
                });

                // 3. Footer Section
                page.Footer().Element(ComposeFooter);
            });
        }

        // --- Helper Methods for Header and Footer ---

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(Model.SchoolName.ToUpper()).FontSize(22).ExtraBold().FontColor(SuccessGreen);
                    col.Item().Text($"Affiliation No: {Model.AffiliationNo ?? "N/A"}").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                    col.Item().Text(Model.SchoolAddress ?? "").FontSize(9);
                    col.Item().Text($"Contact: {Model.SchoolPhone} | Email: {Model.SchoolEmail}").FontSize(9);
                });

                row.ConstantItem(125).AlignCenter().Column(c => {
                    c.Item().Padding(5).Background(SuccessGreen).AlignCenter().Text("FEE RECEIPT").FontSize(9).Bold().FontColor(Colors.White);
                    c.Item().PaddingTop(5).AlignCenter().Text(Model.GenerationDateTime.ToString("dd MMM yyyy")).FontSize(9).SemiBold();
                    c.Item().AlignCenter().Text(Model.GenerationDateTime.ToString("hh:mm tt IST")).FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(c => {
                    c.Item().Text("Note:").FontSize(8).Bold();
                    c.Item().Text("• This is an electronically generated document.").FontSize(7);
                    c.Item().Text("• No signature is required.").FontSize(7);
                    c.Item().PaddingTop(5).Text("Powered by VidyaOS™ Cloud").FontSize(7).Italic();
                });
                row.RelativeItem().AlignRight().Column(c => {
                    c.Item().PaddingTop(25).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    c.Item().AlignCenter().Text("Accounts Department").FontSize(8);
                });
            });
        }

        // --- Styles ---

        static QuestPDF.Infrastructure.IContainer LabelStyle(QuestPDF.Infrastructure.IContainer container)
            => container.PaddingVertical(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium).SemiBold());

        static QuestPDF.Infrastructure.IContainer ValueStyle(QuestPDF.Infrastructure.IContainer container)
            => container.PaddingVertical(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).DefaultTextStyle(x => x.FontSize(10).SemiBold());

        static QuestPDF.Infrastructure.IContainer HeaderStyle(QuestPDF.Infrastructure.IContainer container)
            => container.Background(LightGreenBG).Padding(8).DefaultTextStyle(x => x.FontSize(9).Bold().FontColor(SuccessGreen));

        static QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
            => container.Padding(8).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten4);
    }
}