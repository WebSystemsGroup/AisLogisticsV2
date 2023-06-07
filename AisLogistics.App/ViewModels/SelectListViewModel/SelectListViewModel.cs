using Microsoft.AspNetCore.Mvc.Rendering;

namespace AisLogistics.App.ViewModels.SelectListViewModel
{
    public class SelectListViewModel
    {
        private const int YearStart = 2022;
        public static List<SelectListItem> DocumentTypeListBase = new()
        {
            new("Паспорт гражданина РФ", "21", true),
            new("Свидетельство о рождении", "3", false),
            new("Паспорт иностранного гражданина", "10", false),
            new("Свидетельство о рождении иностранного гражданина", "23", false),
            new("Вид на жительство", "12", false),
            new("Загранпаспорт гражданина РФ", "22", false),
            new("Паспорт моряка", "26", false)
        };

        public static List<SelectListItem> DocumentTypeList = new()
        {
            new ("Не выбрано",""),
            new("Паспорт гражданина РФ", "Паспорт гражданина РФ", true),
            new("Свидетельство о рождении","Свидетельство о рождении", false),
            new("Паспорт иностранного гражданина","Паспорт иностранного гражданина", false),
            new("Вид на жительство","Вид на жительство", false),
            new("Загранпаспорт гражданина РФ","Загранпаспорт гражданина РФ", false),
            new("Паспорт моряка","Паспорт моряка", false)
        };

        public static List<SelectListItem> GenderTypeList = new()
        {
            new ("мужской", "мужской"),
            new ("женский", "женский")
        };

        public static List<SelectListItem> RelationDegreeList = new()
        {
            new ("Не выбрано",""),
            new ("Отец", "Отец"),
            new ("Мать", "Мать"),
            new ("Брат", "Брат"),
            new ("Дядя", "Дядя"),
            new ("Тетя", "Тетя"),
            new ("Знакомые", "Знакомые")
        };

        public static List<SelectListItem> ApplicantTypeList = new() {
            new ("заявитель", "заявитель"),
            new ("законный представитель", "законный представитель"),
        };

        public static List<SelectListItem> MonthTypeList = new() {
            new ("Январь", "1"),
            new ("Февраль", "2"),
            new ("Март", "3"),
            new ("Апрель", "4"),
            new ("Май", "5"),
            new ("Июнь", "6"),
            new ("Июль", "7"),
            new ("Август", "8"),
            new ("Сентябрь", "9"),
            new ("Октябрь", "10"),
            new ("Ноябрь", "11"),
            new ("Декабрь", "12"),
        };

        public static List<SelectListItem> YearTypeListV2 = new() {
            new ("2022", "2022"),
            new ("2023", "2022"),
        };

        public static List<SelectListItem> YearTypeList(bool isCurrentActive) 
        {
            var list = new List<SelectListItem>();
            string value;
            int count = DateTime.Now.AddYears(-YearStart).Year;

            while(count > 0)
            {
                value = DateTime.Now.AddYears(-count).Year.ToString();
                list.Add(new SelectListItem(value, value));
                count--;
            }

            value = DateTime.Now.Year.ToString();

            list.Add(new SelectListItem(value, value, isCurrentActive));

            return list;
        }

    }
}
