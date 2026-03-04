using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VidyaOSDAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🚀 ONLY keep this line to update your live Azure Students table
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Students");
        }
      }
    }
