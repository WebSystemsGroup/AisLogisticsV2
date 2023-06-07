namespace AisLogistics.App.Models
{
    public static class AccessKeysRepository
    {
        public static Dictionary<string, AccessKeyName> AccessKeys = new ()
        {
            {
                AccessKeyNames.EmployeeQueue,
                new AccessKeyNameBuilder("Очередь", AccessKeyNames.EmployeeQueue)
                    .SetAccessGroup(AccessGroup.Default)
                    .SetAccessView()
            },
            {
                AccessKeyNames.DataCase,
                new AccessKeyNameBuilder("Услуги", AccessKeyNames.DataCase)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessView().SetAccessAdd()
            },
            {
                AccessKeyNames.DataCaseIssued,
                new AccessKeyNameBuilder("Услуги на Выдачу", AccessKeyNames.DataCaseIssued)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessView()
            },
            {
                AccessKeyNames.DataCaseOffice,
                new AccessKeyNameBuilder("Услуги Офиса", AccessKeyNames.DataCaseOffice)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessView()
            },
            {
                AccessKeyNames.DataCaseAll,
                new AccessKeyNameBuilder("Все услуги", AccessKeyNames.DataCaseAll)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessView()
            },
            {
                AccessKeyNames.DataCaseComments,
                new AccessKeyNameBuilder("Комментарии", AccessKeyNames.DataCaseComments)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessView().SetAccessAdd()
            },
            {
                AccessKeyNames.DataCaseServiceStage,
                new AccessKeyNameBuilder("Этапы", AccessKeyNames.DataCaseServiceStage)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessView().SetAccessAdd()
            },
            {
                AccessKeyNames.DataCaseServiceAllStage,
                new AccessKeyNameBuilder("Все этапы", AccessKeyNames.DataCaseServiceAllStage)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessAdd()
            },
            {
                AccessKeyNames.DataCaseReestr,
                new AccessKeyNameBuilder("Реестр", AccessKeyNames.DataCaseReestr)
                    .SetAccessGroup(AccessGroup.Data)
                    .SetAccessView().SetAccessAdd()
            },
            {
                AccessKeyNames.ReferenceEmployee,
                new AccessKeyNameBuilder("Сотрудники", AccessKeyNames.ReferenceEmployee)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.ReferenceDocument,
                new AccessKeyNameBuilder("Документы", AccessKeyNames.ReferenceDocument)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.ReferenceService,
                new AccessKeyNameBuilder("Услуги", AccessKeyNames.ReferenceService)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.ReferenceOffice,
                new AccessKeyNameBuilder("Офисы", AccessKeyNames.ReferenceOffice)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.ReferenceRole,
                new AccessKeyNameBuilder("Роли", AccessKeyNames.ReferenceRole)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.ReferenceCalendar,
                new AccessKeyNameBuilder("Календарь", AccessKeyNames.ReferenceCalendar)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessEdit()
            },
            {
                AccessKeyNames.ReferenceFTP,
                new AccessKeyNameBuilder("FTP", AccessKeyNames.ReferenceFTP)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.ReferenceLivingSituation,
                new AccessKeyNameBuilder("Жизненным ситуации", AccessKeyNames.ReferenceLivingSituation)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.ReferenceStateTask,
                new AccessKeyNameBuilder("Гос задание", AccessKeyNames.ReferenceStateTask)
                    .SetAccessGroup(AccessGroup.Reference)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            },
            {
                AccessKeyNames.SystemTerminal,
                new AccessKeyNameBuilder("Терминалы оплаты", AccessKeyNames.SystemTerminal)
                    .SetAccessGroup(AccessGroup.System)
                    .SetAccessView().SetAccessAdd().SetAccessEdit().SetAccessRemove()
            }
        };
    }

    public class AccessKeyName
    {
        public string AccessName { get; set; }
        public string AccessDescription { get; set; }
        public string AccessGroup { get; set; }
        public bool AccessView { get; set; }
        public bool AccessAdd { get; set; }
        public bool AccessEdit { get; set; }
        public bool AccessRemove { get; set; }
    }

    class AccessKeyNameBuilder
    {
        private AccessKeyName AccessKeyName { get; set; }

        public AccessKeyNameBuilder(string description, string name)
        {
            AccessKeyName = new AccessKeyName
            {
                AccessDescription = description,
                AccessName = name,
                AccessGroup = AccessGroup.Default
            };
            
        }

        public AccessKeyNameBuilder SetAccessName(string accessName)
        {
            AccessKeyName.AccessName = accessName;
            return this;
        }

        public AccessKeyNameBuilder SetAccessDescription(string accessDescription)
        {
            AccessKeyName.AccessDescription = accessDescription;
            return this;
        }

        public AccessKeyNameBuilder SetAccessGroup(string accessGroup)
        {
            AccessKeyName.AccessGroup = accessGroup;
            return this;
        }

        public AccessKeyNameBuilder SetAccessView()
        {
            AccessKeyName.AccessView = true;
            return this;
        }

        public AccessKeyNameBuilder SetAccessAdd()
        {
            AccessKeyName.AccessAdd = true;
            return this;
        }

        public AccessKeyNameBuilder SetAccessEdit()
        {
            AccessKeyName.AccessEdit = true;
            return this;
        }

        public AccessKeyNameBuilder SetAccessRemove()
        {
            AccessKeyName.AccessRemove = true;
            return this;
        }

        public static implicit operator AccessKeyName(AccessKeyNameBuilder builder)
        {
            return builder.AccessKeyName;
        }
    }

    public class AccessGroup
    {
        public const string Default = "Прочие";
        public const string Data = "Данные";

        public const string Reference = "Справочники";
        public const string System = "Система";
    }
    public class AccessKeyNames
    {
        public const string DataCase = "AccessDataCase";
        public const string DataCaseIssued = "AccessDataCaseIssued";
        public const string DataCaseOffice = "AccessDataCaseOffice";
        public const string DataCaseAll = "AccessDataCaseAll";
        public const string DataCaseComments = "AccessDataCaseComments";
        public const string DataCaseServiceStage = "AccessDataCaseServiceStage";
        public const string DataCaseServiceAllStage = "AccessDataCaseServiceAllStage";
        public const string DataCaseReestr = "AccessDataCaseReestr";

        public const string EmployeeQueue = "AccessEmployeeQueue";

        public const string ReferenceEmployee = "AccessReferenceEmployee";
        public const string ReferenceDocument = "AccessReferenceDocument";
        public const string ReferenceService = "AccessReferenceService";
        public const string ReferenceOffice = "AccessReferenceOffice";
        public const string ReferenceRole = "AccessReferenceRole";
        public const string ReferenceCalendar = "AccessReferenceCalendar";
        public const string ReferenceFTP = "AccessReferenceFTP";
        public const string ReferenceLivingSituation = "AccessReferenceLivingSituation";
        public const string ReferenceStateTask = "AccessReferenceStateTask";

        //TODO раскомент. когда будут добавлены Claims
        //public const string ReferenceServiceFile = "AccessReferenceServiceFile";
        //public const string ReferenceSmev = "AccessReferenceSmev";
        //public const string ReferenceTosp = "AccessReferenceTosp";
        public const string ReferenceInformation = "AccessReferenceOffice";//"AccessReferenceInformation";

        public const string SystemTerminal = "AccessSystemTerminal";

    }
    public class AccessKeyValues
    {
        public const string View = "View";
        public const string Add = "Add";
        public const string Edit = "Edit";
        public const string Remove = "Remove";
    }
}