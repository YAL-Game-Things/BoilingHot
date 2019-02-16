using System;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace BoilingHot {
    class Program {
        static FieldDefinition GetField(ModuleDefinition module, string type, string name) {
            return module.GetType(type).Fields.First(m => m.Name == name);
        }
        static MethodDefinition GetMethod(ModuleDefinition module, string type, string name) {
            return module.GetType(type).Methods.First(m => m.Name == name);
        }
        static void PatchUpdate(ModuleDefinition module) {
            var update = GetMethod(module, "Unexplored.Core.Grid.DungeonGrid", "Update");
            var processor = update.Body.GetILProcessor();
            var instructions = update.Body.Instructions;
            //
            var insertAt = -1;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.OpCode.Code != Code.Ldfld) continue;
                var field = instruction.Operand as FieldReference;
                if (field == null || field.Name != "Slowed") continue;
                if (field.DeclaringType.FullName != "Unexplored.Core.PlayState") continue;
                insertAt = i;
                break;
            }
            if (insertAt < 0) throw new Exception("Couldn't find where to put the new logic at in DungeonGrid.Update");
            //
            var jumpIf = instructions[insertAt + 1];
            if (jumpIf.OpCode != OpCodes.Brfalse_S) throw new Exception("It would appear like this executable is already patched");
            var locTrue = instructions[insertAt + 2];
            var locFalse = jumpIf.Operand as Instruction;
            var gm_instance = GetField(module, "Unexplored.Core.GameManager", "Instance");
            var gm_player = GetField(module, "Unexplored.Core.GameManager", "Player");
            //
            processor.Replace(jumpIf, processor.Create(OpCodes.Brtrue_S, locTrue));
            // lurge <= 0:
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldsfld, gm_instance));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldfld, gm_player));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldfld,
                GetField(module, "Unexplored.Core.Mobs.Creature", "lurge")));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldc_R4, 0f));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Bgt_Un_S, locFalse));
            // walk <= 0:
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldsfld, gm_instance));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldfld, gm_player));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldfld,
                GetField(module, "Unexplored.Core.Mobs.Creature", "Walk")));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldc_R4, 0f));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Bgt_Un_S, locFalse));
            // LookAheadFactor <= 0:
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldsfld, gm_instance));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldfld, gm_player));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldfld,
                GetField(module, "Unexplored.Core.Mobs.Player", "LookAheadFactor")));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Ldc_R4, 0f));
            processor.InsertBefore(locTrue, processor.Create(OpCodes.Bgt_Un_S, locFalse));
            // we need to ->simplify->optimize-> because some jumps now don't fit into _S range
            update.Body.SimplifyMacros();
            update.Body.OptimizeMacros();
        }
        static void PatchSubmit(ModuleDefinition module) {
            // let's not mess up leaderboards with our hot scores
            var onFindResult = GetMethod(module, "Unexplored.Utils.Leaderboards", "OnLeaderboardFindResult");
            var processor = onFindResult.Body.GetILProcessor();
            processor.InsertBefore(onFindResult.Body.Instructions[0], processor.Create(OpCodes.Ret));
        }
        static void Main(string[] args) {
            var dir = ".";
            var path = dir + "\\Unexplored.exe";
            var orig = dir + "\\Unexplored-Original.exe";
            var next = dir + "\\Unexplored-New.exe";
            if (!File.Exists(path)) {
                Console.WriteLine("Please extract into the game's directory before running.");
            } else {
                try {
                    Console.WriteLine("Making a backup...");
                    if (!File.Exists(orig)) File.Copy(path, orig);
                    //
                    Console.WriteLine("Patching...");
                    //
                    var resolver = new DefaultAssemblyResolver();
                    resolver.AddSearchDirectory(dir);
                    var readerParams = new ReaderParameters();
                    readerParams.AssemblyResolver = resolver;
                    //
                    var module = ModuleDefinition.ReadModule(path, readerParams);
                    PatchUpdate(module);
                    PatchSubmit(module);
                    //
                    Console.WriteLine("Saving...");
                    module.Write(next);
                    //
                    module.Dispose();
                    module = null;
                    File.Delete(path);
                    File.Move(next, path);
                    //
                    Console.WriteLine("All good! You can run the game now.");
                    Console.WriteLine("(and get rid of the patcher's files)");
                } catch (Exception e) {
                    Console.WriteLine("An error occurred: " + e);
                }
            }
            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }
    }
}
