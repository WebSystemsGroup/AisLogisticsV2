﻿using AisLogistics.DataLayer.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AisLogistics.DataLayer.Common.Dto.Reference
{
    public class ServiceDocumentModelDto
    {
        public Guid Id { get; set; }
        public Guid SServicesId { get; set; }


        public Guid? SServicesProcedureId { get; set; }

        /// <summary>
        /// Связь с документами
        /// </summary>
        public Guid SDocumentsId { get; set; }

        /// <summary>
        /// Необходимость документа. Обязательный - 0, Не обязательный документ - 1, При наличии - 2.
        /// </summary>
        public int DocumentNeeds { get; set; }
        /// <summary>
        /// Тип документа. Оригинал - 0, Копия - 1
        /// </summary>
        public int DocumentType { get; set; }
        /// <summary>
        /// Количество копий документа
        /// </summary>
        public int DocumentCount { get; set; }
        
        /// <summary>
        /// Сотрудник добавивший запись
        /// </summary>
        public string EmployeesFioAdd { get; set; }
    }
}
