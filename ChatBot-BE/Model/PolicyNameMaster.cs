using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatBot_BE.Model
{
    public class PolicyNameMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        
        [ForeignKey("PolicyType")]
        public int PolicyTypeId { get; set; }
        
        public required string Name { get; set; }
        
        public PolicyTypeMaster? PolicyType { get; set; }
    }
}
