using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WorkerRole.Models
{
    public class ReaderDocument
    {
        [DisplayName("Website URL")]
        [Required]
        [RegularExpression(@"^(http|https)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(/\S*)?$", ErrorMessage = "Website URL is not valid.")]
        public string URL { get; set; }

        [DisplayName("Email Address")]
        [Required]
        [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "Email Address is not valid.")]
        public string EmailAddress { get; set; }
    }
}
