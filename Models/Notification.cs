using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace AspnetCoreMvcFull.Models
{
  public class Notification
  {
    public int Id { get; set; }

    [DisplayName("Ticket")]
    public int TicketId { get; set; }

    [Required]
    [DisplayName("Title")]
    public string Title { get; set; }

    [Required]
    [DisplayName("Message")]
    public string Message { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:MMM dd yyyy}")]
    [DisplayName("Date")]
    public DateTimeOffset Created { get; set; }

    [Required]
    [DisplayName("Recipient")]
    public string RecipientId { get; set; }

    [Required]
    [DisplayName("Sender")]
    public string SenderId { get; set; }

    [DisplayName("Has been viewed")]
    public bool Viewed { get; set; }

    [DisplayName("Archived")]
    public bool Archived { get; set; }

    //navigation properties
    public virtual Ticket Ticket { get; set; }
    public virtual BTUser Recipient { get; set; }
    public virtual BTUser Sender { get; set; }
  }
}
