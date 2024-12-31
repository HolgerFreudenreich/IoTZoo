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

using Microsoft.Extensions.Primitives;

namespace Domain.Pocos;

public class TopicHistory : BasePoco
{
   public int TopicHistoryId
   {
      get;
      set;
   }

   public string Topic
   {
      get;
      set;
   } = null!;

   public string? Payload
   {
      get;
      set;
   } = null;

   public string? ProjectName
   {
      get;
      set;
   } = null;

   public DateTime DateReceived
   {
      get;
      set;
   } = DateTime.Now;

   // Note: this is important too!
   public override int GetHashCode() => TopicHistoryId.GetHashCode();

   // to display correctly in MudSelect.
   // Alternative: ToStringFunc="@converter"
   //    Func<TopicHistory, string> converter = h => h?.Payload;
   public override string ToString() => Payload ?? string.Empty;

   /// <summary>
   ///  // return Less than zero if this object 
   /// is less than the object specified by the CompareTo method.

   /// return Zero if this object is equal to the object 
   /// specified by the CompareTo method.

   /// return Greater than zero if this object is greater than 
   /// the object specified by the CompareTo method.
   /// </summary>
   /// <param name="obj"></param>
   /// <returns></returns>
   public int CompareTo(object? obj)
   {
      var other = obj as TopicHistory;
      return string.Compare(other?.Payload, Payload, StringComparison.Ordinal);
   }

   public override bool Equals(object? o)
   {
      var other = o as TopicHistory;
      return other?.TopicHistoryId == TopicHistoryId;
   }
}