namespace AisLogistics.App.Models.AdditionalForms;

public class PfrAppointMonthlyCashPaymentsModel : AbstractAdditionalFormModel
{
    public PfrAppointMonthlyCashPaymentsModel()
    {
        Customer = new CustomerType();
        Representative = new RepresentativeType();
        OtherFamilyMembers = new[]
        {
            new OtherPersonType()
        };
    }
    
    /// <summary>
    /// наименование территориального органа Пенсионного фонда Российской Федерации
    /// </summary>
    public string PfrDepartmentName { get; set; }
    
    /// <summary>
    /// Заявитель
    /// </summary>
    public CustomerType Customer { get; set; }
    
    /// <summary>
    /// Представитель
    /// </summary>
    public RepresentativeType Representative { get; set; }
    
    /// <summary>
    /// Место нахождения выплатного дела
    /// </summary>
    public string PaymentCaseLocation { get; set; }
    
    /// <summary>
    /// Категория лица, имеющего право на эжемесячную денежную выплату
    /// </summary>
    public string PersonCategory { get; set; }
    
    /// <summary>
    /// Наименование федерального закона
    /// </summary>
    public string FederalActName { get; set; }
    
    /// <summary>
    /// Дата федерального закона
    /// </summary>
    public string FederalActDate { get; set; }
    
    /// <summary>
    /// наименование территориального органа ПФР, которым будет осуществляться ежемесячная денежная выплата
    /// </summary>
    public string PfrDepartmentPayName { get; set; }

    /// <summary>
    /// Данные о других членах семьи Героя Советского Союза, Героя Российской Федерации
    /// или полного кавалера ордена Славы
    /// </summary>
    public OtherPersonType[] OtherFamilyMembers { get; set; }
    
    /// <summary>
    /// Наименование документа, подтверждающего право на выплаты
    /// </summary>
    public string DocConfirmRightName { get; set; }
    
    /// <summary>
    /// Серия документа, подтверждающего право на выплаты
    /// </summary>
    public string DocConfirmRightSeries { get; set; }
    
    /// <summary>
    /// Номер документа, подтверждающего право на выплаты
    /// </summary>
    public string DocConfirmRightNumber { get; set; }
    
    /// <summary>
    /// Дата выдачи документа, подтверждающего право на выплаты
    /// </summary>
    public DateTime? DocConfirmRightIssueDate { get; set; }
    
    /// <summary>
    /// Кем выдан документ, подтверждающий право на выплаты
    /// </summary>
    public string DocConfirmRightIssuer { get; set; }
    
    /// <summary>
    /// Контантный телефон
    /// </summary>
    public string Phone { get; set; }
    
    /// <summary>
    /// Адрес электронной почты
    /// </summary>
    public string Email { get; set; }
    
    /// <summary>
    /// Получатель инфомарции о ходе и результатах заявления
    /// </summary>
    public string RecipientInformationCode { get; set; }

    /// <summary>
    /// Способ информирования о ходе и результате рассмотрения заявления
    /// 1 - Через «Личный кабинет» на сайте ПФР
    /// 2 - Через Единый портал государственных
    /// 3 - на адрес электронной почты
    /// 4 - на абонентский номер устройства подвижной радиотелефонной связи
    /// Если выбран 3 или 4 - отметить пункт «Путем передачи текстовых сообщений»
    /// </summary>
    public string MethodResultCode { get; set; }
    
    /// <summary>
    /// Контрольный вопрос 
    /// </summary>
    public string SecretQuestionCode { get; set; }
    
    /// <summary>
    /// Ответ на котрольный вопрос 
    /// </summary>
    public string SecretQuestionAnswer { get; set; }
    
    /// <summary>
    /// Секретный код
    /// </summary>
    public string SecretCode { get; set; }

    public abstract class PersonType
    {
        /// <summary>
        /// ФИО / гаименование организации (для представителя - организации)
        /// </summary>
        public string Fio { get; set; }

        /// <summary>
        /// Адрес места жительства
        /// </summary>
        public string ResidenceAddress { get; set; }

        /// <summary>
        /// Адрес места пребывания
        /// </summary>
        public string StayAddress { get; set; }

        /// <summary>
        /// Адрес фактического проживания
        /// </summary>
        public string ActualResidenceAddress { get; set; }

        /// <summary>
        /// Наименование ДУЛ
        /// </summary>
        public string DocName { get; set; }

        /// <summary>
        /// Серия и номер ДУЛ
        /// </summary>
        public string DocSeriesNumber { get; set; }

        /// <summary>
        /// Дата выдачи ДУЛ
        /// </summary>
        public DateTime? DocIssueDate { get; set; }

        /// <summary>
        /// Кем выдан ДУЛ
        /// </summary>
        public string DocIssuer { get; set; }
    }

    public class CustomerType : PersonType
    {
        /// <summary>
        /// Фамилия, которая была при рождении
        /// </summary>
        public string LastNameBirth { get; set; }

        /// <summary>
        /// СНИЛС
        /// </summary>
        public string Snils { get; set; }
        
        /// <summary>
        /// Гражданство
        /// </summary>
        public string Citizenship { get; set; }

        /// <summary>
        /// Пол
        /// </summary>
        public int? Gender { get; set; }
        
        /// <summary>
        /// Дата рождения
        /// </summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// Место рождения
        /// </summary>
        public string BirthPlace { get; set; }
    }

    public class OtherPersonType
    { 
        /// <summary>
        /// ФИО
        /// </summary>
        public string Fio { get; set; }
        
        /// <summary>
        /// Дата рождения
        /// </summary>
        public DateTime? BirthDate { get; set; }
        
        /// <summary>
        /// Степень родства
        /// </summary>
        public string Relation { get; set; }
    }
    
    public class RepresentativeType : PersonType
    {
        /// <summary>
        /// юридический адрес организации 
        /// </summary>
        public string OrganizationLegalAddress { get; set; } 
        
        /// <summary>
        /// Место нахождения организации
        /// </summary>
        public string OrganizationLocation { get; set; }
        
        /// <summary>
        /// Наименование документа, подтверждающего полномочия представителя
        /// </summary>
        public string AuthorityDocName { get; set; } 
        
        /// <summary>
        /// Серия и номер документа, подтверждающего полномочия представителя
        /// </summary>
        public string AuthorityDocSeriesNumber { get; set; } 
        
        /// <summary>
        /// Дата выдачи документа, подтверждабщего полномочия представителя
        /// </summary>
        public DateTime? AuthorityDocIssueDate { get; set; }
        
        /// <summary>
        /// Кем выдан документ, подтверждающий полномочия представителя
        /// </summary>
        public string AuthorityDocIssuer { get; set; }
        
    }
}