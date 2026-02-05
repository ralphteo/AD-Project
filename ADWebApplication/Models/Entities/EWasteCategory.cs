using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADWebApplication.Models;

[Table("ewasteCategory")]
public class EWasteCategory
{
    [Key]
    [Column("categoryId")]
    public int CategoryId {get; set;}

    [Column("categoryName")]
    public string CategoryName {get; set; } = "";

    public ICollection<EWasteItemType> EWasteItemTypes {get; set;} = new List<EWasteItemType>();
}
