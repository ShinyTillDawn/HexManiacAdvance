﻿using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;


namespace HavenSoft.HexManiac.Integration {
   public class IntegrationTests {
      public static Singletons singletons { get; } = new Singletons();
      private static readonly string fireredName = "sampleFiles/Pokemon FireRed.gba";
      private static readonly string emeraldName = "sampleFiles/Pokemon Emerald.gba";
      private static readonly Lazy<IDataModel> lazyFireRed, lazyEmerald;

      public StubFileSystem FileSystem { get; } = new();
      public List<string> Errors { get; } = new();
      public List<string> Messages { get; } = new();

      static IntegrationTests() {
         lazyFireRed = new Lazy<IDataModel>(() => 
            new HardcodeTablesModel(singletons, File.ReadAllBytes(fireredName), new StoredMetadata(new string[0]))
         );
         lazyEmerald = new Lazy<IDataModel>(() =>
            new HardcodeTablesModel(singletons, File.ReadAllBytes(emeraldName), new StoredMetadata(new string[0]))
         );
      }

      protected ViewPort LoadFireRed() {
         Skip.IfNot(File.Exists(fireredName));
         var model = new HardcodeTablesModel(singletons, File.ReadAllBytes(fireredName), new StoredMetadata(new string[0]));
         var vm = new ViewPort(fireredName, model, InstantDispatch.Instance, singletons, new(), FileSystem) { MaxDiffSegmentCount = 10 };
         vm.OnError += (sender, e) => Errors.Add(e);
         vm.OnMessage += (sender, e) => Messages.Add(e);
         vm.MapEditor.OnError += (sender, e) => Errors.Add(e);
         vm.MapEditor.OnMessage += (sender, e) => Messages.Add(e);
         return vm;
      }

      protected ViewPort LoadEmerald() {
         Skip.IfNot(File.Exists(emeraldName));
         var model = new HardcodeTablesModel(singletons, File.ReadAllBytes(emeraldName), new StoredMetadata(new string[0]));
         var vm = new ViewPort(emeraldName, model, InstantDispatch.Instance, singletons, new(), FileSystem) { MaxDiffSegmentCount = 10 };
         vm.OnError += (sender, e) => Errors.Add(e);
         vm.OnMessage += (sender, e) => Messages.Add(e);
         vm.MapEditor.OnError += (sender, e) => Errors.Add(e);
         vm.MapEditor.OnMessage += (sender, e) => Messages.Add(e);
         return vm;
      }

      /// <summary>
      /// Only use this method if the test will make no changes to the model (data or metadata).
      /// </summary>
      protected ViewPort LoadReadOnlyFireRed() {
         Skip.IfNot(File.Exists(fireredName));
         var vm = new ViewPort(
            fireredName,
            lazyFireRed.Value,
            InstantDispatch.Instance,
            singletons, new(),
            new StubFileSystem()
         ) {
            MaxDiffSegmentCount = 10
         };
         vm.OnError += (sender, e) => Errors.Add(e);
         vm.OnMessage += (sender, e) => Messages.Add(e);
         vm.MapEditor.OnError += (sender, e) => Errors.Add(e);
         vm.MapEditor.OnMessage += (sender, e) => Messages.Add(e);
         return vm;
      }

      /// <summary>
      /// Only use this method if the test will make no changes to the model (data or metadata).
      /// </summary>
      protected ViewPort LoadReadOnlyEmerald() {
         Skip.IfNot(File.Exists(emeraldName));
         var vm = new ViewPort(
            emeraldName,
            lazyEmerald.Value,
            InstantDispatch.Instance,
            singletons, new(),
            new StubFileSystem()
         ) {
            MaxDiffSegmentCount = 10
         };
         vm.OnError += (sender, e) => Errors.Add(e);
         vm.OnMessage += (sender, e) => Messages.Add(e);
         vm.MapEditor.OnError += (sender, e) => Errors.Add(e);
         vm.MapEditor.OnMessage += (sender, e) => Messages.Add(e);
         return vm;
      }

      protected static void AssertNoConflicts(IViewPort viewPort) {
         var model = viewPort.Model as PokemonModel;
         model.ResolveConflicts();
      }
   }
}
