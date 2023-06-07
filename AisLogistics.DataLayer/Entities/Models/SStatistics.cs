using System;

namespace AisLogistics.DataLayer.Entities.Models
{
    public partial class SStatistics
    {
        /// <summary>
        /// Первичный ключ
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Наименование статистики
        /// </summary>
        public string StatisticsName { get; set; }
        /// <summary>
        /// Связь с статистикой группы
        /// </summary>
        public Guid SStatisticsGroupId { get; set; }
        /// <summary>
        /// Путь к файлу в АИС
        /// </summary>
        public string StatisticsPath { get; set; }
        /// <summary>
        /// Сортировка по важности
        /// </summary>
        public int StatisticsOrder { get; set; }
        /// <summary>
        /// Описание
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// Активность отчета
        /// </summary>
        public bool IsActive { get; set; }
        public virtual SStatisticsGroup SStatisticsGroups { get; set; }
    }
}
