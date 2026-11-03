using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class RenameFiscalYearProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CLRプロパティ名のみの変更。DB列FiscalYearと一意インデックスは維持する。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // DBスキーマの変更がないため、取り消すDDLもない。
        }
    }
}
