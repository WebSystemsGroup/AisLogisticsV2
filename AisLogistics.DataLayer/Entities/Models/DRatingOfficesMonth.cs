using System;
using System.Collections.Generic;

namespace AisLogistics.DataLayer.Entities.Models
{
    /// <summary>
    /// Рейтинг по  филиалам за месяц.
    /// </summary>
    public partial class DRatingOfficesMonth
    {
        /// <summary>
        /// Первичный ключ
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Min значение по принятым услугам
        /// </summary>
        public int MinReceived { get; set; }
        /// <summary>
        /// Max значение по принятым услугам
        /// </summary>
        public int MaxRecived { get; set; }
        /// <summary>
        /// Min значение по исполненым услугам
        /// </summary>
        public int MinExecution { get; set; }
        /// <summary>
        /// Max значение по исполненым услугам
        /// </summary>
        public int MaxExecution { get; set; }
        /// <summary>
        /// Min значение по просроченным этапам
        /// </summary>
        public decimal MinPercentOverdue { get; set; }
        /// <summary>
        /// Max значение по просроченным этапам
        /// </summary>
        public decimal MaxPercentOverdue { get; set; }
        /// <summary>
        /// Нормализованное значение по принятым услугам
        /// </summary>
        public decimal NormalizedRecived { get; set; }
        /// <summary>
        /// Нормализованное значение по исполненым услугам
        /// </summary>
        public decimal NormalizedExecution { get; set; }
        /// <summary>
        /// Нормализованное значение по просроченным этапам
        /// </summary>
        public decimal NormalizedOverdue { get; set; }
        /// <summary>
        /// Среднее нормализованное значение
        /// </summary>
        public decimal NormalizedAverage { get; set; }
        /// <summary>
        /// Позиция по рейтингу 
        /// </summary>
        public int RatingPosition { get; set; }
        /// <summary>
        /// перемещение по позиции -1 вниз 0 на месте 1 вверх
        /// </summary>
        public int RatingMoving { get; set; }
        /// <summary>
        /// Филиал
        /// </summary>
        public Guid SOfficesId { get; set; }
        /// <summary>
        /// Филиал
        /// </summary>
        public string OfficesName { get; set; }
        /// <summary>
        /// Месяц
        /// </summary>
        public int Month { get; set; }
        /// <summary>
        /// Год
        /// </summary>
        public int Year { get; set; }
        /// <summary>
        /// Дата
        /// </summary>
        public DateTime RatingDate { get; set; }
    }
}
