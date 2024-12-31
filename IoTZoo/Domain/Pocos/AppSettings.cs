// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace Domain.Pocos;

public enum DatabaseType
{
   Postgres = 0,
   Sqlite = 1
}

public class PhilipsHueBridgeSettings
{
   public string Ip
   {
      get;
      set;
   } = null!;

   public string Key
   {
      get;
      set;
   } = null!;
}
   
public class AppSettings
{
   public bool UseInternalMqttBroker
   {
      get;
      set;
   }

   public string MqttBrokerIp
   {
      get;
      set;
   } = string.Empty;

   public string MqttBrokerUser
   {
      get;
      set;
   } = string.Empty;

   public int MqttBrokerPort
   {
      get;
      set;
   } = 1883;

   public double Latitude
   {
      get;
      set;
   } = 52.629968125136294;

   public double Longitude
   {
      get;
      set;
   } = 9.61393118304487;

   public string ConnectionStringPostgres
   {
      get;
      set;
   } = string.Empty;

   public string ConnectionStringSqlite
   {
      get;
      set;
   } = string.Empty;

   public string ConnectionStringSqliteDatabaseTopicHistory
   {
      get;
      set;
   } = string.Empty;

   public string ConnectionStringSqliteDatabaseComponentManagement
   {
      get;
      set;
   } = string.Empty;

   public string ConnectionStringSqliteDatabaseScripting
   {
      get;
      set;
   } = string.Empty;

   public string ConnectionStringSqliteDatabaseSettings
   {
      get;
      set;
   } = string.Empty;

   public DatabaseType ConfigurationDatabaseType
   {
      get;
      set;
   } = DatabaseType.Sqlite;

}