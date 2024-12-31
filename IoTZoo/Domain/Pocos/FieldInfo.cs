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

/// <summary>
/// Represents a database field.
/// </summary>
public class FieldInfo
{
   public string? DatabaseName
   {
      get;
      set;
   }
   public string SchemaName
   {
      get;
      set;
   } = string.Empty;

   public string? TableName
   {
      get;
      set;
   } = string.Empty;

   public string ColumnName
   {
      get;
      set;
   } = string.Empty;

   public Type? DataType
   {
      get;
      set;
   }

   public int? FieldLength
   {
      get;
      set;
   }

   public bool IsNullable
   {
      get;
      set;
   }

   public bool IsAutoIncrement
   {
      get;
      set;
   }

   public int Ordinal
   {
      get;
      set;
   }

   public bool IsUnique
   {
      get;
      set;
   }

   public bool IsKey
   {
      get;
      set;
   }
}