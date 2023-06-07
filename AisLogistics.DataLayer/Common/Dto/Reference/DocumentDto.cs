using System;

namespace AisLogistics.DataLayer.Common.Dto.Reference
{
    public class DocumentDto
    {

        /// <summary>
        /// Первичный ключ
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Наименование документа
        /// </summary>
        public string Name { get; set; }
    }
}
