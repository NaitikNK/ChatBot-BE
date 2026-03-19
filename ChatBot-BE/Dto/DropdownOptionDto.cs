namespace ChatBot_BE.Dto
{
    public class DropdownOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
