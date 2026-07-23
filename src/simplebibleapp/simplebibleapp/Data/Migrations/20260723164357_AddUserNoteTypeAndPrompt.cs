using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace simplebibleapp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNoteTypeAndPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NoteType",
                table: "UserNotes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Prompt",
                table: "UserNotes",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoteType",
                table: "UserNotes");

            migrationBuilder.DropColumn(
                name: "Prompt",
                table: "UserNotes");
        }
    }
}
