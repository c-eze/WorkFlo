using Microsoft.AspNetCore.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace AspnetCoreMvcFull.Models
{
  public class BTUser : IdentityUser
  {
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName { get { return $"{FirstName} {LastName}"; } }

    [NotMapped]
    [DataType(DataType.Upload)]
    public IFormFile? AvatarFormFile { get; set; }

    [DisplayName("File Name")]
    public string? AvatarFileName { get; set; }

    public byte[]? AvatarFileData { get; set; }


    [DisplayName("File Extension")]
    public string? AvatarContentType { get; set; }

    public int CompanyId { get; set; }

    //Navigation properties
    public virtual Company Company { get; set; }
    public virtual ICollection<Project> Projects { get; set; }
  }
}
