using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Net.Sockets;

namespace AspnetCoreMvcFull.Models
{
  public class Project
  {
    public int Id { get; set; }

    [DisplayName("Company")]
    public int CompanyId { get; set; }

    [Required]
    [StringLength(50)]
    [DisplayName("Project Name")]
    public string Name { get; set; }


    [DisplayName("Description")]
    public string Description { get; set; }


    [DisplayName("Start Date")]
    [DataType(DataType.Date)]
    public DateTimeOffset StartDate { get; set; }


    [DisplayName("End Date")]
    [DataType(DataType.Date)]
    public DateTimeOffset EndDate { get; set; }


    [DisplayName("Priority")]
    public int? ProjectPriorityId { get; set; }

    [NotMapped]
    [DataType(DataType.Upload)]
    public IFormFile? ImageFormFile { get; set; }

    [DisplayName("File Name")]
    public string? ImageFileName { get; set; }

    [DisplayName("Image")]
    public byte[]? ImageFileData { get; set; }


    [DisplayName("File Extension")]
    public string? ImageContentType { get; set; }

    [DisplayName("Archived")]
    public bool Archived { get; set; }

    //navigation properties
    public virtual Company Company { get; set; }

    [DisplayName("Priority")]
    public virtual ProjectPriority ProjectPriority { get; set; }
    public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
  }
}
