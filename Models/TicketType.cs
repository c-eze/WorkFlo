using System.ComponentModel;

namespace AspnetCoreMvcFull.Models
{
  public class TicketType
  {
    public int Id { get; set; }

    [DisplayName("Type")]
    public string Name { get; set; }
  }
}
