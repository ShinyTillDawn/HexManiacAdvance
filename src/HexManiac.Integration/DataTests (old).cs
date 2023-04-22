﻿using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Visitors;
using HavenSoft.HexManiac.Tests;
using System.IO;
using Xunit;

namespace HavenSoft.HexManiac.Integration {
   public class DataTests : IntegrationTests {
      [SkippableFact]
      public void PokemonNameEnum_3ByteValue_NoTooltipError() {
         var firered = LoadReadOnlyFireRed();
         var pokenames = firered.Model.Get<ArrayRun>(HardcodeTablesModel.PokemonNameTable);

         ToolTipContentVisitor.GetEnumImage(firered.Model, new(), 1000000, pokenames);

         // no crash = pass
      }

      [SkippableFact]
      public void FireRed_ChangeLengthOfTypeNamesToS_NoAssert() {
         var firered = LoadFireRed();

         firered.Goto.Execute(HardcodeTablesModel.TypesTableName);
         var text = firered.AnchorText;
         text = text.Split("]")[0] + ']';
         firered.AnchorText = text + "s";

         // no debug assert -> pass
      }

      [SkippableFact]
      public void BrockScript_ExpandSelection_EntireScriptSelected() {
         var firered = LoadReadOnlyFireRed();
         firered.Goto.Execute("data.maps.banks/6/maps/2/map/0/events/0/objects/0/script/");

         firered.ExpandSelection(firered.SelectionStart.X, firered.SelectionStart.Y);

         var firstAddress = firered.ConvertViewPointToAddress(firered.SelectionStart);
         var lastAddress = firered.ConvertViewPointToAddress(firered.SelectionEnd);
         var length = lastAddress - firstAddress + 1;
         Assert.Equal(171, length);
      }

      [SkippableFact]
      public void UseTypeSwapScript_UpgradeVersion_TableStillIncluded() {
         var contents = File.ReadAllBytes("resources/scripts/ability_type_swaps.hma");
         var firered = LoadFireRed();
         firered.TryImport(new("script.hma", contents), FileSystem);

         var refTable = singletons.GameReferenceTables[firered.Model.GetGameCode()];
         var metadata = firered.Model.ExportMetadata(refTable, new StubMetadataInfo { VersionNumber = "0.5" });
         var newModel = new PokemonModel(firered.Model.RawData, metadata, singletons);

         var table = newModel.GetTableModel("data.abilities.typeswaps");
         Assert.NotNull(table);
      }

      [SkippableFact]
      public void DefaultMetadata_DefaultHashesForAllAnchors() {
         var firered = LoadReadOnlyFireRed();

         var refTable = singletons.GameReferenceTables[firered.Model.GetGameCode()];
         var metadata = firered.Model.ExportMetadata(refTable, singletons.MetadataInfo);

         Assert.All(metadata.NamedAnchors, anchor => Assert.NotEmpty(anchor.Hash));
      }

      [SkippableFact]
      public void UpdateList_UpgradeVersion_ListKeepsChanges() {
         var firered = LoadFireRed();
         firered.Model.TryGetList("owfootprints", out var list); // 3 items
         list.Add("custom");
         firered.Model.SetList(firered.CurrentChange, "owfootprints", list, list.StoredHash);

         var refTable = singletons.GameReferenceTables[firered.Model.GetGameCode()];
         var metadata = firered.Model.ExportMetadata(refTable, singletons.MetadataInfo);
         var newModel = new PokemonModel(firered.Model.RawData, metadata, singletons);

         newModel.TryGetList("owfootprints", out list);
         Assert.Equal("custom", list[3]);
      }

      [SkippableFact]
      public void Duplicate_Trainer_Debug_Assert() {
         var emerald = LoadEmerald();
         emerald.Goto.Execute("data.trainers.stats/33");
         emerald.SelectionStart = new Point(0, 0); // from Haven
         emerald.SelectionEnd = new Point(emerald.Width - 1, 0); // The row width at the trainer stats table defaults to 40 (base X)
         emerald.Copy.Execute(FileSystem);
         string copy = FileSystem.CopyText;

         emerald.Goto.Execute("data.trainers.stats/34");
         emerald.Edit(copy);

         ((PokemonModel)emerald.Model).ResolveConflicts();
      }

<<<<<<< HEAD
=======
      [SkippableFact]
      public void Emerald_ChangeTrainerPokemonPointerToUsedAddress_NoAssert() {
         var emerald = LoadEmerald();

         emerald.Goto.Execute("data.trainers.stats/marcel/pokemon");
         emerald.Edit("<00030B>");

         // no debug assert = pass
      }
>>>>>>> upstream/master
   }
}
