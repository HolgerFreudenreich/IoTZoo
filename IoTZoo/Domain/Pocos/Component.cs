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

public class Component : BasePoco
{
   public int ComponentId
   {
      get;
      set;
   }

   public string Sku { get; set; } = null!;

   public string Description
   {
      get;
      set;
   } = null!;


   // Note: this is important too!
   public override int GetHashCode() => ComponentId.GetHashCode();

   // to display correctly in MudSelect.
   // Alternative: ToStringFunc="@converter"
   //    Func<Planum, string> converter = p => p?.PlanumName;
   public override string ToString() => Sku;

   /// <summary>
   /// return Less than zero if this object is less than the object specified by the CompareTo method.
   /// return Zero if this object is equal to the object 
   /// specified by the CompareTo method.
   /// return Greater than zero if this object is greater than the object specified by the CompareTo method.
   /// </summary>
   /// <param name="obj"></param>
   /// <returns></returns>
   public int CompareTo(object? obj)
   {
      var other = obj as Component;
      if (other == null)
      {
         return 0;
      }
      return string.Compare(other.Sku, Description, StringComparison.Ordinal);
   }

   public override bool Equals(object? o)
   {
      var other = o as Component;
      return other?.ComponentId == ComponentId;
   }
}