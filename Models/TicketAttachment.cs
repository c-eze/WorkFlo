using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using AspnetCoreMvcFull.Extensions;

namespace AspnetCoreMvcFull.Models
{
  public class TicketAttachment
  {
    public int Id { get; set; }

    [DisplayName("Ticket")]
    public int TicketId { get; set; }

    [DisplayName("File Date")]
    public DateTimeOffset Created { get; set; }

    [DisplayName("Team Member")]
    public string? UserId { get; set; }

    [DisplayName("File Description")]
    public string Description { get; set; }

    [NotMapped]
    [DisplayName("Select a file")]
    [DataType(DataType.Upload)]
    [MaxFileSize(1024 * 1024)]
    [AllowedExtensions(new string[] { ".jpg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".ppt", ".pptx", ".html", ".txt" })]
    public IFormFile FormFile { get; set; }

    [DisplayName("File Name")]
    public string? FileName { get; set; }
    public byte[]? FileData { get; set; }

    [DisplayName("File Extension")]
    public string? FileContentType { get; set; }

    //Navigation Properties
    public virtual Ticket? Ticket { get; set; }
    public virtual BTUser? User { get; set; }
  }
}
