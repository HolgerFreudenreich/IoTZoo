// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

namespace DeveloperTests;

public interface IPrepareSourceCodeForGit
{
   void Execute();
   void SetFileHeaders();
}

/// <summary>
/// Prepares the source code for GIT commit.
/// - Removes Network credentials
/// - Sets file Headers to *.cs files
/// - Removes Philips HUE settings
/// - Empties/ Truncates databases
/// </summary>
public class PrepareSourceCodeForGitCheckinService : IPrepareSourceCodeForGit
{
   public void Execute()
   {
      SetFileHeaders();
   }

   public void SetFileHeaders()
   {
      string fileHeader = File.ReadAllText("FileHeader.txt");

      var files = Directory.GetFiles("..\\..\\..\\..\\..\\", "*.cs", SearchOption.AllDirectories);
      foreach (var file in files)
      {
         if (!file.Contains("SunriseCalculator", StringComparison.OrdinalIgnoreCase))
         {
            var sourceCode = File.ReadAllText(file);
            if (!sourceCode.StartsWith("// -------"))
            {
               sourceCode = sourceCode.Insert(0, fileHeader);
               File.WriteAllText(file, sourceCode);
            }
         }
      }

   }
}
