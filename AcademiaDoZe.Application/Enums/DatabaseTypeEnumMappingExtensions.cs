using AcademiaDoZe.Infrastructure.Data;
namespace AcademiaDoZe.Application.Enums;
public static class DatabaseTypeEnumMappingExtensions
{
public static DatabaseType ToInfrastructure(this AppDatabaseType appDatabaseType)
{
return (DatabaseType)appDatabaseType;
}
public static AppDatabaseType ToApplication(this DatabaseType databaseType)
{
return (AppDatabaseType)databaseType;
}
}