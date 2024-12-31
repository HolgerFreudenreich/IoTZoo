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

using Domain.Pocos;

namespace Domain.Interfaces.Crud
{
   public interface ISettingsCrudService
   {
      /// <summary>
      /// Delivers the Setting for <see cref="settingsKey"/>.
      /// </summary>
      /// <param name="settingsKey"></param>
      /// <returns></returns>
      public Task<Setting?> GetSetting(string category, string settingsKey);

      public Task<bool> GetSettingBool(SettingCategory category, SettingKey settingsKey);

      public Task<bool> GetSettingBool(string category, string settingsKey);

      public Task<double> GetSettingDouble(SettingCategory category, SettingKey settingsKey);

      public Task<double> GetSettingDouble(string category, string settingsKey);

      public Task<string> GetSettingString(SettingCategory category, SettingKey settingsKey);

      public Task<string> GetSettingString(string category, string settingsKey);

      public Task<object?> GetObject(SettingCategory category, SettingKey settingsKey);

      public Task<int> Update(SettingCategory category,
                              SettingKey key,
                              object value);
      public Task<int> Update(SettingCategory category,
                              SettingKey key,
                              string value);
      public Task<int> Update(SettingCategory category,
                              SettingKey key,
                              double value);
      /// <summary>
      /// Saves a setting entry (insert or update).
      /// </summary>
      /// <param name="setting"></param>
      public Task<int> Update(Setting setting);

      public Task<int> Insert(Setting setting);

      public Task<int> Delete(Setting setting);
   }
}