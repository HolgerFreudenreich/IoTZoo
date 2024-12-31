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

public class KnownTopic : BasePoco
{
   private string topic = null!;

   public int KnownTopicId
   {
      get;
      set;
   }

   public int? ParentKnownTopicId
   {
      get;
      set;
   }

   public string NamespaceName
   {
      get;
      set;
   } = string.Empty;

   public string ProjectName
   {
      get;
      set;
   } = string.Empty;

   public bool? AllowDelete
   {
      get; set;
   }

   public bool? AllowEdit
   {
      get; set;
   }

   public string Topic
   {
      get
      {
         return topic;
      }

      set
      {
         topic = value;
      }
   }

   public string FullQualifiedTopic
   {
      get
      {
         if (string.IsNullOrEmpty(NamespaceName))
         {
            if (string.IsNullOrEmpty(ProjectName))
            {
               return Topic;
            }
            return $"{ProjectName}/{Topic}";
         }
         return $"{NamespaceName}/{ProjectName}/{Topic}";
      }
   }

   public string? Description
   {
      get;
      set;
   }

   /// <summary>
   /// True, if the received topic should be persisted in table topic_history.
   /// </summary>
   public bool KeepHistory
   {
      get;
      set;
   }

   public bool? Retained
   {
      get;
      set;
   }

   public MessageDirection MessageDirection
   {
      get;
      set;
   } = MessageDirection.Unknown;

   public string? LastPayload
   {
      get;
      set;
   }

   /// <summary>
   /// Device which sends this topic
   /// </summary>
   public string? Sender
   {
      get;
      set;
   } = null;

   public DateTime? PayloadUpdatedAt
   {
      get;
      set;
   }

   // Note: this is important too!
   public override int GetHashCode() => KnownTopicId.GetHashCode();

   // to display correctly in MudSelect.

   public override string ToString() => Topic;


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
      var other = obj as KnownTopic;
      return string.Compare(other?.Topic, Topic, StringComparison.Ordinal);
   }

   public override bool Equals(object? other)
   {
      var otherKnownTopic = other as KnownTopic;
      return otherKnownTopic?.KnownTopicId == KnownTopicId;
   }
}