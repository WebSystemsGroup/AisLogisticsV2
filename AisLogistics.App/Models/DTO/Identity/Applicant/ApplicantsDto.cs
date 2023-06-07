﻿using AisLogistics.App.ViewModels.Cases;
using AisLogistics.DataLayer.Common.Dto.Case;

namespace AisLogistics.App.Models.DTO.Identity.Applicant
{
    public class ApplicantsDto
    {
        /// Первичный ключ
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Номер дела заявителя
        /// </summary>
        public string DCasesId { get; set; }
        /// <summary>
        /// Связь со справочником документов, удостоверяющих личность заявителя
        /// </summary>
        public int SDocumentsIdentityId { get; set; }
        /// <summary>
        /// Пол заявителя (1 - мужской, 2 - женский)
        /// </summary>
        public int CustomerGender { get; set; }
        /// <summary>
        /// Номер документа
        /// </summary>
        public string DocumentNumber { get; set; }
        /// <summary>
        /// Серия документа
        /// </summary>
        public string DocumentSerial { get; set; }
        /// <summary>
        /// Дата рождения
        /// </summary>
        public DateTime? DocumentBirthDate { get; set; }
        /// <summary>
        /// Место рождения
        /// </summary>
        public string DocumentBirthPlace { get; set; }
        /// <summary>
        /// Дата выдачи паспорта
        /// </summary>
        public DateTime? DocumentIssueDate { get; set; }
        /// <summary>
        /// Наименование отделения, выдавшего паспорт
        /// </summary>
        public string DocumentIssueBody { get; set; }
        /// <summary>
        /// Код документа
        /// </summary>
        public string DocumentCode { get; set; }
        /// <summary>
        /// ИНН
        /// </summary>
        public string CustomerInn { get; set; }
        /// <summary>
        /// Номер телефона 1
        /// </summary>
        public string CustomerPhone1 { get; set; }
        /// <summary>
        /// Номер телефона 2
        /// </summary>
        public string CustomerPhone2 { get; set; }
        /// <summary>
        /// Электронная почта заявителя
        /// </summary>
        public string CustomerEmail { get; set; }
        /// <summary>
        /// Адрес регистрации
        /// </summary>
        public string CustomerAddress { get; set; }
        /// <summary>
        /// ОКАТО
        /// </summary>
        public string CustomerOkato { get; set; }
        /// <summary>
        /// ОКТМО
        /// </summary>
        public string CustomerOktmo { get; set; }
        /// <summary>
        /// КОД региона
        /// </summary>
        public string CustomerCodeRegion { get; set; }
        /// <summary>
        /// СНИЛС Заявителя
        /// </summary>
        public string CustomerSnils { get; set; }
        /// <summary>
        /// Тип заявителя (1 - Заявитель, 2 - Представитель)
        /// </summary>
        public int CustomerType { get; set; }
        /// <summary>
        /// Согласие на участие в региональном опросе
        /// </summary>
        public bool PollRegionSms { get; set; }
        /// <summary>
        /// Согласие на участие ИАС МКГУ (0 - отказ, 1 - СМС, 2 - Через теминал)
        /// </summary>
        public int PollIasMkgu { get; set; }
        /// <summary>
        /// Способ оповещения, связь со справочником оповещений
        /// </summary>
        public int SAlertCustomerId { get; set; }
        public string FirstName { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// Отчество
        /// </summary>
        public string SecondName { get; set; }
        /// <summary>
        /// Адрес проживания
        /// </summary>
        public string CustomerAddressResidence { get; set; }
        /// <summary>
        /// Дата временной регистрации
        /// </summary>
        public DateTime? DateTempRegistration { get; set; }
        /// <summary>
        /// Адрес регистрации временный
        /// </summary>
        public string CustomerAddressTemp { get; set; }
        public AddressDetails? addressDetails { get; set; }
    }
}
