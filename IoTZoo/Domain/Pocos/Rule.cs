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

public enum TriggerCondition
{
   Always = 0,
   FireOnSourcePayloadChanged = 1,
}

public class Rule : BasePoco
{
   public int RuleId
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

   /// <summary>
   /// Topic which triggers the rule.
   /// </summary>  
   public string? SourceTopic
   {
      get;
      set;
   }

   /// <summary>
   /// Topic which triggers the rule.
   /// </summary>  
   public string? SourceTopicFullQualified
   {
      get
      {
         if (null == SourceTopic)
         {
            return null;
         }
         return $"{NamespaceName}/{ProjectName}/{SourceTopic}";
      }
   }

   /// <summary>
   /// Topic which depends on the rule result. if the expression is validated to true, than the topic will be published with success_value.
   /// </summary>
   public string? TargetTopic
   {
      get;
      set;
   }

   /// <summary>
   /// Topic which depends on the rule result. if the expression is validated to true, than the topic will be published with success_value.
   /// </summary>  
   public string? TargetTopicFullQualified
   {
      get
      {
         if (null == TargetTopic)
         {
            return null;
         }
         return $"{NamespaceName}/{ProjectName}/{TargetTopic}";
      }
   }

   /// <summary>
   /// The expression to be validated.
   /// </summary>
   public string? Expression
   {
      get;
      set;
   }

   public string? ExpressionEvaluationProtocol { get; set; } = null!;

   public bool ExpressionEvaluationResult { get; set; }


   /// <summary>
   /// value to publish after successfully validated.
   /// </summary>
   public string? TargetPayload
   {
      get;
      set;
   }

   /// <summary>
   /// true, if the expression can be interpreted.
   /// </summary>
   public bool? IsValid
   {
      get;
      set;
   }

   public bool Enabled
   {
      get;
      set;
   } = true;

   /// <summary>
   /// Zero is the highest Priority.
   /// </summary>
   public int Priority
   {
      get;
      set;
   } = 5;

   public TriggerCondition TriggerCondition
   {
      get;
      set;
   }

   public int DelayMs
   {
      get;
      set;
   }

   public DateTime LastTriggerDateTime
   {
      get;
      set;
   }

   public int? RuleAuditGroupId
   {
      get;
      set;
   }

   public string? PropertyMapping
   {
      get; set;
   }
}