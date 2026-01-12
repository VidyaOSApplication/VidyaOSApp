using VidyaOSDAL.DTOs;

public static class FeeReceiptTemplate
{
    public static string GetHtml(FeeReceiptResponse r)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial; }}
        .header {{ text-align:center; font-size:18px; font-weight:bold; }}
        .section {{ margin-top:15px; }}
        table {{ width:100%; border-collapse:collapse; }}
        td {{ padding:6px; }}
        .border td {{ border:1px solid #000; }}
    </style>
</head>
<body>

<div class='header'>{r.SchoolName}</div>
<div style='text-align:center'>{r.SchoolAddress}</div>
<hr/>

<div class='section'>
<b>Receipt No:</b> {r.ReceiptNo}<br/>
<b>Date:</b> {r.ReceiptDate:dd-MMM-yyyy}
</div>

<div class='section'>
<b>Student:</b> {r.StudentName}<br/>
<b>Admission No:</b> {r.AdmissionNo}<br/>
<b>Class:</b> {r.ClassSection}
</div>

<table class='border section'>
<tr>
    <td><b>Fee Month</b></td>
    <td>{r.FeeMonth}</td>
</tr>
<tr>
    <td><b>Amount Paid</b></td>
    <td>₹ {r.Amount}</td>
</tr>
<tr>
    <td><b>Payment Mode</b></td>
    <td>{r.PaymentMode}</td>
</tr>
</table>

<div class='section' style='text-align:right'>
<b>Authorized Signature</b>
</div>

</body>
</html>";
    }
}
