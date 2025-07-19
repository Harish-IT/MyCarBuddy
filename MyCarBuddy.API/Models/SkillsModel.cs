namespace MyCarBuddy.API.Models
{
    public class SkillsModel
    {
        public int? SkillID { get; set; }
        public string SkillName {  get; set; }
        public string Description {  get; set; }
        public int? CreatedBy { get; set; }
        public int ModifiedBy { get; set; }
        public bool? IsActive { get; set; }
    }
}
