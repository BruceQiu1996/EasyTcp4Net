using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileTransfer.Migrations
{
    /// <inheritdoc />
    public partial class AddFileReceiveRecordModelTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransferToken",
                table: "FileSendRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransferToken",
                table: "FileSendRecords",
                type: "TEXT",
                nullable: true);
        }
    }
}
