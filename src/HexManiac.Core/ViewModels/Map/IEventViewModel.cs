﻿using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using Microsoft.Scripting.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace HavenSoft.HexManiac.Core.ViewModels.Map {

   public interface IEventViewModel : IEquatable<IEventViewModel>, INotifyPropertyChanged {
      event EventHandler EventVisualUpdated;
      public event EventHandler<EventCycleDirection> CycleEvent;
      public ICommand CycleEventCommand { get; }
      string EventType { get; }
      string EventIndex { get; }
      int TopOffset { get; }
      int LeftOffset { get; }
      int X { get; set; }
      int Y { get; set; }
      IPixelViewModel EventRender { get; }
      void Render(IDataModel model, LayoutModel layout);
      bool Delete();
   }

   public enum EventCycleDirection { PreviousCategory, PreviousEvent, NextEvent, NextCategory, None }

   public class FlyEventViewModel : ViewModelCore, IEventViewModel {
      private readonly ModelArrayElement flySpot;
      private readonly ModelArrayElement connectionEntry;

      public string EventType => "Fly";

      public string EventIndex => "1/1";

      public virtual int TopOffset => 0;
      public virtual int LeftOffset => 0;

      #region X/Y

      public int X {
         get => !Valid ? -1 : flySpot.GetValue("x");
         set {
            if (!Valid) return;
            flySpot.SetValue("x", value);
            NotifyPropertyChanged();
            if (!ignoreUpdateXY) xy = null;
            NotifyPropertyChanged(nameof(XY));
            EventVisualUpdated.Raise(this);
         }
      }

      public int Y {
         get => !Valid ? -1 : flySpot.GetValue("y");
         set {
            if (!Valid) return;
            flySpot.SetValue("y", value);
            NotifyPropertyChanged();
            if (!ignoreUpdateXY) xy = null;
            NotifyPropertyChanged(nameof(XY));
            EventVisualUpdated.Raise(this);
         }
      }

      private bool ignoreUpdateXY;
      private string xy;
      public string XY {
         get {
            if (!Valid) return "(-1, -1)";
            if (xy == null) xy = $"({X}, {Y})";
            return xy;
         }
         set {
            if (!Valid) return;
            xy = value;
            var parts = value.Split(new[] { ',', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return;
            ignoreUpdateXY = true;
            if (parts[0].TryParseInt(out int x)) X = x;
            if (parts[1].TryParseInt(out int y)) Y = y;
            ignoreUpdateXY = false;
         }
      }


      #endregion

      public IPixelViewModel EventRender { get; private set; }

      public bool Valid { get; }

      private StubCommand cycleEventCommand;
      public ICommand CycleEventCommand => StubCommand<EventCycleDirection>(ref cycleEventCommand, direction => {
         CycleEvent.Raise(this, direction);
      });

      public event EventHandler EventVisualUpdated;
      public event EventHandler<EventCycleDirection> CycleEvent;

      public static IEnumerable<FlyEventViewModel> Create(IDataModel model, int bank, int map, Func<ModelDelta> tokenFactory) {
         var flyTable = model.GetTableModel(HardcodeTablesModel.FlySpawns, tokenFactory);
         if (flyTable == null) yield break;
         for (int i = 0; i < flyTable.Count; i++) {
            var flight = flyTable[i];
            if (flight.GetValue("bank") != bank) continue;
            if (flight.GetValue("map") != map) continue;
            yield return new FlyEventViewModel(flight, bank, map, i + 1);
         }
      }

      public FlyEventViewModel(ModelArrayElement flySpot, int bank, int map, int expectedFlight) {
         this.flySpot = flySpot;
         var model = flySpot.Model;
         var tokenFactory = () => flySpot.Token;
         // get the region from the map
         var banks = model.GetTableModel(HardcodeTablesModel.MapBankTable, tokenFactory);
         if (banks == null) return; // not valid map table
         var maps = banks[bank].GetSubTable("maps");
         if (maps == null) return;  // not valid bank
         var table = maps[map].GetSubTable("map");
         if (table == null) return; // not valid map
         var region = table[0].GetValue(Format.RegionSection);
         if (model.IsFRLG()) region -= 88;
         if (region < 0) return;    // not valid region section

         Valid = true; // connection entries are optional

         var flyIndexTable = model.GetTableModel(HardcodeTablesModel.FlyConnections, tokenFactory);
         if (flyIndexTable == null) return;
         if (region >= flyIndexTable.Count) return;
         var entry = flyIndexTable[region];
         if (entry.TryGetValue("flight", out int savedFlight) && savedFlight == expectedFlight) {
            connectionEntry = entry;
         }
      }

      /// <returns>true if the event was deleted</returns>
      public bool Delete() {
         if (!Valid) return false;
         if (connectionEntry == null) return false; // cannot delete 'special' fly events (such as the player's house)
         // set the connection table's index to 0
         connectionEntry.SetValue("flight", 0);
         flySpot.SetValue("bank", 0);
         flySpot.SetValue("map", 0);
         flySpot.SetValue("x", 0);
         flySpot.SetValue("y", 0);
         return true;
      }

      public bool Equals(IEventViewModel? other) {
         if (other is not FlyEventViewModel fly) return false;
         return X == fly.X && Y == fly.Y && flySpot.Start == fly.flySpot.Start;
      }

      public void Render(IDataModel model, LayoutModel layout) {
         EventRender = BaseEventViewModel.BuildEventRender(UncompressedPaletteColor.Pack(31, 31, 0));
      }
   }

   public abstract class BaseEventViewModel : ViewModelCore, IEventViewModel, IEquatable<IEventViewModel> {
      public event EventHandler EventVisualUpdated;
      public event EventHandler<EventCycleDirection> CycleEvent;

      private StubCommand cycleEventCommand;
      public ICommand CycleEventCommand => StubCommand<EventCycleDirection>(ref cycleEventCommand, direction => {
         CycleEvent?.Invoke(this, direction);
      });

      protected readonly ModelArrayElement element;
      private readonly string parentLengthField;

      public ModelArrayElement Element => element;
      public ModelDelta Token => element.Token;

      public string EventType => GetType().Name.Replace("EventViewModel", " Event");
      public string EventIndex => $"{element.ArrayIndex + 1} / {element.Table.ElementCount}";
      public virtual int TopOffset => 0;
      public virtual int LeftOffset => 0;

      #region X/Y

      public int X {
         get => element.GetValue("x");
         set {
            element.SetValue("x", value);
            NotifyPropertyChanged();
            if (!ignoreUpdateXY) xy = null;
            NotifyPropertyChanged(nameof(XY));
            RaiseEventVisualUpdated();
         }
      }

      public int Y {
         get => element.GetValue("y");
         set {
            element.SetValue("y", value);
            NotifyPropertyChanged();
            if (!ignoreUpdateXY) xy = null;
            NotifyPropertyChanged(nameof(XY));
            RaiseEventVisualUpdated();
         }
      }

      private bool ignoreUpdateXY;
      private string xy;
      public string XY {
         get {
            if (xy == null) xy = $"({X}, {Y})";
            return xy;
         }
         set {
            xy = value;
            var parts = value.Split(new[] { ',', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return;
            ignoreUpdateXY = true;
            if (parts[0].TryParseInt(out int x)) X = x;
            if (parts[1].TryParseInt(out int y)) Y = y;
            ignoreUpdateXY = false;
         }
      }

      #endregion

      #region Elevation

      public int Elevation {
         get => element.GetValue("elevation");
         set => element.SetValue("elevation", value);
      }

      #endregion

      public IPixelViewModel EventRender { get; protected set; }

      public BaseEventViewModel(ModelArrayElement element, string parentLengthField) => (this.element, this.parentLengthField) = (element, parentLengthField);

      public bool Delete() => DeleteElement(parentLengthField);

      public virtual bool Equals(IEventViewModel other) {
         if (other is not BaseEventViewModel bem) return false;
         return bem.element.Start == element.Start;
      }

      public abstract void Render(IDataModel model, LayoutModel layout);

      protected void RaiseEventVisualUpdated() => EventVisualUpdated.Raise(this);

      protected bool DeleteElement(string parentCountField) {
         var table = element.Table;
         var model = element.Model;
         var token = element.Token;
         var offset = table.ConvertByteOffsetToArrayOffset(element.Start);
         var editCount = table.ElementCount - offset.ElementIndex - 1;
         for (int i = 0; i < editCount; i++) {
            int segmentOffset = 0;
            for (int j = 0; j < table.ElementContent.Count; j++) {
               var source = element.Start + (i + 1) * element.Length + segmentOffset;
               var destination = source - element.Length;
               var length = table.ElementContent[j].Length;
               if (table.ElementContent[j].Type == ElementContentType.Pointer) {
                  model.UpdateArrayPointer(token, table.ElementContent[j], table.ElementContent, offset.ElementIndex, destination, model.ReadPointer(source));
               } else {
                  model.WriteMultiByteValue(destination, length, token, model.ReadMultiByteValue(source, length));
               }
               segmentOffset += length;
            }
         }
         if (table.ElementCount > 1) {
            var shorterTable = table.Append(token, -1);
            model.ObserveRunWritten(token, shorterTable);
         } else {
            foreach (var source in table.PointerSources) {
               model.UpdateArrayPointer(token, null, null, 0, source, Pointer.NULL);
               if (model.GetNextRun(source) is ITableRun parentTable) {
                  var parent = new ModelArrayElement(model, parentTable.Start, 0, () => token, parentTable);
                  parent.SetValue(parentCountField, 0);
               }
            }
            model.ClearFormatAndData(token, table.Start, table.Length);
         }
         return true;
      }

      protected string GetText(int pointer) {
         if (pointer == Pointer.NULL) return null;
         var address = element.Model.ReadPointer(pointer);
         if (address < 0 || address >= element.Model.Count) return null;
         var run = element.Model.GetNextRun(address);
         if (run.Start != address) {
            if (run.Start < address) return null;
            var length = PCSString.ReadString(element.Model, address, true);
            if (run.Start < address + length || length < 1) return null;
            // we can add a PCSRun here
            run = new PCSRun(element.Model, address, length, SortedSpan.One(pointer));
            if (element.Model.GetNextRun(pointer).Start >= pointer + 4) element.Model.ObserveRunWritten(element.Token, new PointerRun(pointer));
            element.Model.ObserveRunWritten(element.Token, run);
         }
         if (run is not PCSRun pcs) {
            var length = PCSString.ReadString(element.Model, address, true);
            element.Model.ClearFormat(element.Token, address, length);
            pcs = new PCSRun(element.Model, address, length, run.PointerSources);
         }
         if (pcs.Length < 1) return string.Empty;
         return pcs.SerializeRun();
      }

      protected int SetText(int pointer, string text, [CallerMemberName] string propertyName = null) {
         if (pointer == Pointer.NULL) return Pointer.NULL;
         var address = element.Model.ReadPointer(pointer);
         if (address < 0 || address >= element.Model.Count) return -1;
         if (element.Model.GetNextRun(address) is not PCSRun pcs) return -1;
         var newRun = pcs.DeserializeRun(text, element.Token, out _, out _);
         element.Model.ObserveRunWritten(element.Token, newRun);
         NotifyPropertyChanged(propertyName);
         return newRun.Start != pcs.Start ? newRun.Start : -1;
      }

      protected string GetAddressText(int address, ref string field) {
         if (field == null) {
            field = $"<{address.ToAddress()}>";
            if (address == Pointer.NULL) field = "<null>";
         }
         return field;
      }

      protected void SetAddressText(string value, ref string field, string fieldName) {
         field = value;
         value = field.Trim("<nul> ".ToCharArray());
         element.SetAddress(fieldName, value.TryParseHex(out int result) ? result : Pointer.NULL);
      }

      private static readonly Point[] focalPoints = new[] { new Point(0, 7), new Point(7, 0), new Point(15, 8), new Point(8, 15) };
      public static IPixelViewModel BuildEventRender(short color, bool indentSides = false) {
         var pixels = new short[256];
         
         for (int x = 1; x < 15; x++) {
            for (int y = 1; y < 15; y++) {
               if (((x + y) & 1) != 0) continue;
               if (indentSides && focalPoints.Any(p => Math.Abs(p.X - x) + Math.Abs(p.Y - y) < 4)) continue;
               pixels[y * 16 + x] = color;
               y++;
            }
         }
         return new ReadonlyPixelViewModel(new SpriteFormat(4, 2, 2, default), pixels, transparent: 0);
      }
   }

   public class ObjectEventViewModel : BaseEventViewModel {
      private readonly ScriptParser parser;
      private readonly BerryInfo berries;
      private readonly Action<int> gotoAddress;

      public event EventHandler<DataMovedEventArgs> DataMoved;

      public int Start => element.Start;

      public int ObjectID {
         get => element.GetValue("id");
         set {
            element.SetValue("id", value);
            NotifyPropertyChanged();
         }
      }

      public int Graphics {
         get => element.GetValue("graphics");
         set {
            element.SetValue("graphics", value);
            RaiseEventVisualUpdated();
            NotifyPropertyChanged();
         }
      }

      /// <summary>
      /// FireRed Only.
      /// Kind is either 0 or 255.
      /// If it's 255, then this is an 'offscreen' object, which is a copy of an object in a connected map.
      /// The trainerType and trainerRangeOrBerryID have the map and bank information, respectively.
      /// </summary>
      public bool HasKind => element.HasField("kind");
      public bool Kind {
         get => element.TryGetValue("kind", out int value) ? value != 0 : false;
         set {
            if (element.HasField("kind")) element.SetValue("kind", value ? 0xFF : 0);
         }
      }

      public int MoveType {
         get => element.GetValue("moveType");
         set {
            element.SetValue("moveType", value);
            RaiseEventVisualUpdated();
            NotifyPropertyChanged();
         }
      }

      #region Range

      public int RangeX {
         get => element.GetValue("range") & 0xF;
         set {
            element.SetValue("range", (RangeY << 4) | value);
            rangeXY = null;
            NotifyPropertyChanged(nameof(RangeXY));
            RaiseEventVisualUpdated();
         }
      }

      public int RangeY {
         get => element.GetValue("range") >> 4;
         set {
            element.SetValue("range", (value << 4) | RangeX);
            rangeXY = null;
            NotifyPropertyChanged(nameof(RangeXY));
            RaiseEventVisualUpdated();
         }
      }

      private string rangeXY;
      public string RangeXY {
         get {
            if (rangeXY == null) rangeXY = $"({RangeX}, {RangeY})";
            return rangeXY;
         }
         set {
            rangeXY = value;
            var parts = value.Split(new[] { ',', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return;
            if (parts[0].TryParseInt(out int x) && parts[1].TryParseInt(out int y)) element.SetValue("range", (y << 4) | x);
            NotifyPropertyChanged(nameof(RangeX));
            NotifyPropertyChanged(nameof(RangeY));
            NotifyPropertyChanged();
            RaiseEventVisualUpdated();
         }
      }

      #endregion

      public int TrainerType {
         get => element.GetValue("trainerType");
         set {
            element.SetValue("trainerType", value);
            NotifyPropertyChanged(nameof(ShowBerryContent));
            NotifyPropertyChanged();
         }
      }

      public int TrainerRangeOrBerryID {
         get => element.GetValue("trainerRangeOrBerryID");
         set {
            element.SetValue("trainerRangeOrBerryID", value);
            RaiseEventVisualUpdated();
            NotifyPropertiesChanged(nameof(ShowBerryContent), nameof(BerryText));
            NotifyPropertyChanged();
         }
      }

      public int ScriptAddress {
         get => element.GetAddress("script");
         set {
            element.SetAddress("script", value);
            NotifyPropertyChanged();
            trainerSprite = null;
            scriptAddressText = npcText =
               martContentText = martHello = martGoodbye =
               trainerAfterText = trainerBeforeText = trainerWinText =
               tutorFailedText = tutorInfoText = tutorSuccessText = tutorWhichPokemonText =
               tradeFailedText = tradeInitialText = tradeSuccessText = tradeThanksText = null;
            NotifyPropertiesChanged(
               nameof(ScriptAddressText),
               nameof(ShowItemContents), nameof(ItemContents),
               nameof(ShowNpcText), nameof(NpcText),
               nameof(ShowTrainerContent), nameof(TrainerClass), nameof(TrainerSprite), nameof(TrainerName), nameof(TrainerBeforeText), nameof(TrainerAfterText), nameof(TrainerWinText), nameof(TrainerTeam),
               nameof(ShowMartContents), nameof(MartHello), nameof(MartContent), nameof(MartGoodbye),
               nameof(ShowTutorContent), nameof(TutorInfoText), nameof(TutorWhichPokemonText), nameof(TutorFailedText), nameof(TutorSucessText), nameof(TutorNumber),
               nameof(ShowTradeContent), nameof(TradeFailedText), nameof(TradeIndex), nameof(TradeInitialText), nameof(TradeSuccessText), nameof(TradeThanksText), nameof(TradeWrongSpeciesText),
               nameof(ShowBerryContent), nameof(BerryText));
         }
      }

      public void GotoScript() => gotoAddress(ScriptAddress);
      public bool CanGotoScript => 0 <= ScriptAddress && ScriptAddress < element.Model.Count;

      private string scriptAddressText;
      public string ScriptAddressText {
         get {
            if (scriptAddressText != null) return scriptAddressText;
            var value = element.GetAddress("script");
            return GetAddressText(value, ref scriptAddressText);
         }
         set {
            SetAddressText(value, ref scriptAddressText, "script");
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(ScriptAddress));
         }
      }

      public int Flag {
         get => element.GetValue("flag");
         set {
            element.SetValue("flag", value);
            NotifyPropertyChanged();
            flagText = null;
            NotifyPropertyChanged(nameof(FlagText));
         }
      }

      string flagText;
      public string FlagText {
         get {
            if (flagText == null) flagText = element.GetValue("flag").ToString("X4");
            return flagText;
         }
         set {
            flagText = value;
            element.SetValue("flag", value.TryParseHex(out int result) ? result : 0);
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(Flag));
         }
      }

      public IPixelViewModel DefaultOW { get; }
      public ObservableCollection<VisualComboOption> Options { get; } = new();
      public ObservableCollection<string> FacingOptions { get; } = new();
      public ObservableCollection<string> ClassOptions { get; } = new();
      public ObservableCollection<string> ItemOptions { get; } = new();

      #region Extended Properties

      // For certain simple events (npcs, trainers, items, signposts),
      // We can provide an enriched editing experience in the event panel.
      // These are the 'show' properties for those controls.

      public bool ShowItemContents => EventTemplate.GetItemAddress(element.Model, this) != Pointer.NULL;

      public int ItemContents {
         get {
            var itemAddress = EventTemplate.GetItemAddress(element.Model, this);
            if (itemAddress == Pointer.NULL) return -1;
            return element.Model.ReadMultiByteValue(itemAddress, 2);
         }
         set {
            var itemAddress = EventTemplate.GetItemAddress(element.Model, this);
            if (itemAddress == Pointer.NULL) return;
            element.Model.WriteMultiByteValue(itemAddress, 2, element.Token, value);
            NotifyPropertyChanged();
         }
      }

      public bool ShowNpcText => EventTemplate.GetNPCTextPointer(element.Model, this) != Pointer.NULL;

      private string npcText;
      public string NpcText {
         get {
            if (npcText != null) return npcText;
            return npcText = GetText(EventTemplate.GetNPCTextPointer(element.Model, this));
         }
         set {
            npcText = value;
            var newStart = SetText(EventTemplate.GetNPCTextPointer(element.Model, this), value);
            if (newStart != -1) DataMoved.Raise(this, new("Text", newStart));
         }
      }

      #region Trainer Content

      public bool ShowTrainerContent => EventTemplate.GetTrainerContent(element.Model, this) != null;

      public int TrainerClass {
         get {
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return -1;
            return element.Model[trainerContent.TrainerClassAddress];
         }
         set {
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return;
            element.Token.ChangeData(element.Model, trainerContent.TrainerClassAddress, (byte)value);
         }
      }

      private IPixelViewModel trainerSprite;
      public IPixelViewModel TrainerSprite {
         get {
            if (trainerSprite != null) return trainerSprite;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return null;
            var spriteIndex = element.Model[trainerContent.TrainerClassAddress + 2];
            var spriteAddress = element.Model.GetTableModel(HardcodeTablesModel.TrainerSpritesName)[spriteIndex].GetAddress("sprite");
            var spriteRun = element.Model.GetNextRun(spriteAddress) as ISpriteRun;
            return trainerSprite = ReadonlyPixelViewModel.Create(element.Model, spriteRun, true, .5);
         }
      }

      private string trainerName;
      public string TrainerName {
         get {
            if (trainerName != null) return trainerName;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return null;
            var text = element.Model.TextConverter.Convert(element.Model, trainerContent.TrainerNameAddress, 12);
            return trainerName = text.Trim('"');
         }
         set {
            trainerName = value;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return;
            var bytes = element.Model.TextConverter.Convert(value, out _);
            while (bytes.Count > 12) {
               bytes.RemoveAt(bytes.Count - 1);
               bytes[bytes.Count - 1] = 0xFF;
            }
            while (bytes.Count < 12) bytes.Add(0);
            element.Token.ChangeData(element.Model, trainerContent.TrainerNameAddress, bytes);
            NotifyPropertyChanged();
         }
      }

      private string trainerBeforeText;
      public string TrainerBeforeText {
         get {
            if (trainerBeforeText != null) return trainerBeforeText;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return null;
            return trainerBeforeText = GetText(trainerContent.BeforeTextPointer);
         }
         set {
            trainerBeforeText = value;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return;
            var newStart = SetText(trainerContent.BeforeTextPointer, value);
            if (newStart != -1) DataMoved.Raise(this, new("Text", newStart));
         }
      }

      private string trainerWinText;
      public string TrainerWinText {
         get {
            if (trainerWinText != null) return trainerWinText;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return null;
            return trainerWinText = GetText(trainerContent.WinTextPointer);
         }
         set {
            trainerWinText = value;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return;
            var newStart = SetText(trainerContent.WinTextPointer, value);
            if (newStart != -1) DataMoved.Raise(this, new("Text", newStart));
         }
      }

      private string trainerAfterText;
      public string TrainerAfterText {
         get {
            if (trainerAfterText != null) return trainerAfterText;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return null;
            return trainerAfterText = GetText(trainerContent.AfterTextPointer);
         }
         set {
            trainerAfterText = value;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return;
            var newStart = SetText(trainerContent.AfterTextPointer, value);
            if (newStart != -1) DataMoved.Raise(this, new("Text", newStart));
         }
      }

      private string teamText;
      public string TrainerTeam {
         get {
            if (teamText != null) return teamText;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return null;
            var address = element.Model.ReadPointer(trainerContent.TeamPointer);
            if (address < 0 || address >= element.Model.Count) return null;
            if (element.Model.GetNextRun(address) is not TrainerPokemonTeamRun run) return null;
            if (run.Start != address) return null;
            if (TeamVisualizations.Count == 0) UpdateTeamVisualizations(run);
            return teamText = run.SerializeRun();
         }
         set {
            teamText = value;
            var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
            if (trainerContent == null) return;
            var address = element.Model.ReadPointer(trainerContent.TeamPointer);
            if (address < 0 || address >= element.Model.Count) return;
            if (element.Model.GetNextRun(address) is not TrainerPokemonTeamRun run) return;
            if (run.Start != address) return;
            var newRun = run.DeserializeRun(value, element.Token, false, false, out _);
            element.Model.ObserveRunWritten(element.Token, newRun);
            if (newRun.Start != run.Start) DataMoved.Raise(this, new("Trainer Team", newRun.Start));
            UpdateTeamVisualizations(newRun);
            NotifyPropertyChanged();
         }
      }

      public ObservableCollection<IPixelViewModel> TeamVisualizations { get; } = new();

      private void UpdateTeamVisualizations(TrainerPokemonTeamRun team) {
         TeamVisualizations.Clear();
         foreach (var vis in team.Visualizations) {
            TeamVisualizations.Add(vis);
         }
      }

      private StubCommand openTrainerData;
      public ICommand OpenTrainerData => StubCommand(ref openTrainerData, () => {
         var trainerContent = EventTemplate.GetTrainerContent(element.Model, this);
         if (trainerContent == null) return;
         gotoAddress(trainerContent.TrainerClassAddress - 1);
      });

      #endregion

      #region Mart Content

      private Lazy<MartEventContent> martContent;

      public bool ShowMartContents => martContent.Value != null;

      private string martHello, martContentText, martGoodbye;
      public string MartHello {
         get => GetText(ref martHello, martContent.Value?.HelloPointer);
         set => SetText(ref martHello, martContent.Value?.HelloPointer, value, "Text");
      }

      public string MartContent {
         get {
            if (martContentText != null) return martContentText;
            if (martContent.Value == null) return null;
            var martStart = element.Model.ReadPointer(martContent.Value.MartPointer);
            if (element.Model.GetNextRun(martStart) is not IStreamRun stream) return null;
            var lines = stream.SerializeRun().SplitLines().Select(line => line.Trim('"'));
            return martContentText = Environment.NewLine.Join(lines);
         }
         set {
            martContentText = value;
            if (martContent.Value == null) return;
            var martStart = element.Model.ReadPointer(martContent.Value.MartPointer);
            if (element.Model.GetNextRun(martStart) is not IStreamRun stream) return;
            var newStream = stream.DeserializeRun(value, Token, out var _, out var _);
            element.Model.ObserveRunWritten(Token, newStream);
            if (newStream.Start != stream.Start) DataMoved.Raise(this, new("Mart", newStream.Start));
         }
      }

      public string MartGoodbye {
         get => GetText(ref martGoodbye, martContent.Value?.GoodbyePointer);
         set => SetText(ref martGoodbye, martContent.Value?.GoodbyePointer, value, "Text");
      }

      #endregion

      #region Tutor Content

      private Lazy<TutorEventContent> tutorContent;

      public bool ShowTutorContent {
         get {
            var content = tutorContent.Value;
            if (content != null && TutorOptions.Count == 0) {
               TutorOptions.AddRange(element.Model.GetOptions(HardcodeTablesModel.MoveTutors));
            }
            return tutorContent.Value != null;
         }
      }

      private string tutorInfoText, tutorWhichPokemonText, tutorFailedText, tutorSuccessText;
      public string TutorInfoText {
         get => GetText(ref tutorInfoText, tutorContent.Value?.InfoPointer);
         set => SetText(ref tutorInfoText, tutorContent.Value?.InfoPointer, value, "Text");
      }

      public string TutorWhichPokemonText {
         get => GetText(ref tutorWhichPokemonText, tutorContent.Value?.WhichPokemonPointer);
         set => SetText(ref tutorWhichPokemonText, tutorContent.Value?.WhichPokemonPointer, value, "Text");
      }

      public string TutorFailedText {
         get => GetText(ref tutorFailedText, tutorContent.Value?.FailedPointer);
         set => SetText(ref tutorFailedText, tutorContent.Value?.FailedPointer, value, "Text");
      }

      public string TutorSucessText {
         get => GetText(ref tutorSuccessText, tutorContent.Value?.SuccessPointer);
         set => SetText(ref tutorSuccessText, tutorContent.Value?.SuccessPointer, value, "Text");
      }

      public int TutorNumber {
         get {
            if (tutorContent.Value == null) return -1;
            return element.Model.ReadMultiByteValue(tutorContent.Value.TutorAddress, 2);
         }
         set {
            if (tutorContent.Value == null) return;
            element.Model.WriteMultiByteValue(tutorContent.Value.TutorAddress, 2, Token, value);
         }
      }

      public ObservableCollection<string> TutorOptions { get; } = new();

      public void GotoTutors() => gotoAddress(element.Model.GetTableModel(HardcodeTablesModel.MoveTutors)[TutorNumber].Start);

      #endregion

      #region Trade Content

      private Lazy<TradeEventContent> tradeContent;

      public ObservableCollection<string> TradeOptions { get; } = new();

      public bool ShowTradeContent {
         get {
            var content = tradeContent.Value;
            if (content != null && TradeOptions.Count == 0) {
               var pokenames = element.Model.GetOptions(HardcodeTablesModel.PokemonNameTable);
               foreach (var trade in element.Model.GetTableModel(HardcodeTablesModel.TradeTable)) {
                  if (!trade.TryGetValue("receive", out int receive) || !trade.TryGetValue("give", out int give)) {
                     TradeOptions.Add(TradeOptions.Count.ToString());
                  } else {
                     TradeOptions.Add($"{pokenames[give]} -> {pokenames[receive]}");
                  }
               }
            }
            return tradeContent.Value != null;
         }
      }

      private string tradeInitialText, tradeThanksText, tradeSuccessText, tradeFailedText, tradeWrongSpeciesText;
      public string TradeInitialText {
         get => GetText(ref tradeInitialText, tradeContent.Value?.InfoPointer);
         set => SetText(ref tradeInitialText, tradeContent.Value?.InfoPointer, value, "Text");
      }

      public string TradeThanksText {
         get => GetText(ref tradeThanksText, tradeContent.Value?.ThanksPointer);
         set => SetText(ref tradeThanksText, tradeContent.Value?.ThanksPointer, value, "Text");
      }

      public string TradeSuccessText {
         get => GetText(ref tradeSuccessText, tradeContent.Value?.SuccessPointer);
         set => SetText(ref tradeSuccessText, tradeContent.Value?.SuccessPointer, value, "Text");
      }

      public string TradeFailedText {
         get => GetText(ref tradeFailedText, tradeContent.Value?.FailedPointer);
         set => SetText(ref tradeFailedText, tradeContent.Value?.FailedPointer, value, "Text");
      }

      public string TradeWrongSpeciesText {
         get => GetText(ref tradeWrongSpeciesText, tradeContent.Value?.WrongSpeciesPointer);
         set => SetText(ref tradeWrongSpeciesText, tradeContent.Value?.WrongSpeciesPointer, value, "Text");
      }

      public int TradeIndex {
         get {
            if (tradeContent.Value == null) return -1;
            return element.Model.ReadMultiByteValue(tradeContent.Value.TradeAddress, 2);
         }
         set {
            if (tradeContent.Value == null) return;
            element.Model.WriteMultiByteValue(tradeContent.Value.TradeAddress, 2, Token, value);
         }
      }

      public void GotoTrades() => gotoAddress(element.Model.GetTableModel(HardcodeTablesModel.TradeTable)[TradeIndex].Start);

      #endregion

      #region Berry Content

      public bool ShowBerryContent => TrainerType == 0 && TrainerRangeOrBerryID != 0;

      public string BerryText {
         get {
            if (berries.BerryMap.TryGetValue(TrainerRangeOrBerryID, out BerrySpot spot)) {
               if (spot.BerryID >= 0 && spot.BerryID < berries.BerryOptions.Count) {
                  return berries.BerryOptions[spot.BerryID];
               }
            }
            return "Unknown";
         }
      }

      public void GotoBerryCode() {
         if (berries.BerryMap.TryGetValue(TrainerRangeOrBerryID, out BerrySpot spot)) {
            gotoAddress(spot.Address);
         }
      }

      #endregion

      private string GetText(ref string cache, int? pointer) {
         if (cache != null) return cache;
         if (pointer == null) return null;
         return cache = GetText((int)pointer);
      }

      private void SetText(ref string cache, int? pointer, string value, string type, [CallerMemberName] string propertyName = null) {
         cache = value;
         if (pointer == null) return;
         var newStart = SetText((int)pointer, value, propertyName);
         if (newStart != -1) DataMoved.Raise(this, new(type, newStart));
      }

      #endregion

      public ObjectEventViewModel(ScriptParser parser, Action<int> gotoAddress, ModelArrayElement objectEvent, IReadOnlyList<IPixelViewModel> sprites, IPixelViewModel defaultSprite, BerryInfo berries) : base(objectEvent, "objectCount") {
         this.parser = parser;
         this.gotoAddress = gotoAddress;
         this.berries = berries;
         for (int i = 0; i < sprites.Count; i++) Options.Add(VisualComboOption.CreateFromSprite(i.ToString(), sprites[i].PixelData, sprites[i].PixelWidth, i, 2));
         DefaultOW = defaultSprite;
         objectEvent.Model.TryGetList("FacingOptions", out var list);
         foreach (var item in list) FacingOptions.Add(item);
         foreach (var item in objectEvent.Model.GetOptions(HardcodeTablesModel.TrainerClassNamesTable)) ClassOptions.Add(item);
         foreach (var item in objectEvent.Model.GetOptions(HardcodeTablesModel.ItemsTableName)) ItemOptions.Add(item);

         tutorContent = new Lazy<TutorEventContent>(() => EventTemplate.GetTutorContent(element.Model, parser, this));
         martContent = new Lazy<MartEventContent>(() => EventTemplate.GetMartContent(element.Model, parser, this));
         tradeContent = new Lazy<TradeEventContent>(() => EventTemplate.GetTradeContent(element.Model, parser, this));
      }

      public override int TopOffset => 16 - (EventRender?.PixelHeight ?? 0);
      public override int LeftOffset => (16 - (EventRender?.PixelWidth ?? 0)) / 2;

      public override void Render(IDataModel model, LayoutModel layout) {
         var ows = model.GetTable(HardcodeTablesModel.OverworldSprites);
         var owTable = ows == null ? null : new ModelTable(model, ows.Start);
         var facing = MoveType switch {
            7 => 1,
            9 => 2,
            10 => 3,
            _ => 0,
         };
         EventRender = Render(model, owTable, DefaultOW, Graphics, facing);
         NotifyPropertyChanged(nameof(EventRender));
      }

      /// <param name="facing">(0, 1, 2, 3) = (down, up, left, right)</param>
      public static IPixelViewModel Render(IDataModel model, ModelTable owTable, IPixelViewModel defaultOW, int index, int facing) {
         if (owTable == null || index >= owTable.Count) return defaultOW;
         var element = owTable[index];
         var data = element.GetSubTable("data")[0];
         var sprites = data.GetSubTable("sprites");
         if (sprites == null) return defaultOW;
         bool flip = facing == 3;
         if (facing == 3) facing = 2;
         if (facing >= sprites.Count) facing = 0;
         var graphicsAddress = sprites.Run.Start;
         var pointerAddress = data.Start;
         var graphicsRun = model.GetNextRun(graphicsAddress) as ISpriteRun;
         var paletteRun = graphicsRun.FindRelatedPalettes(model, pointerAddress).FirstOrDefault();
         if (facing != -1) {
            var sprite = sprites[facing];
            graphicsAddress = sprite.GetAddress("sprite");
            graphicsRun = model.GetNextRun(graphicsAddress) as ISpriteRun;
         }
         if (graphicsRun == null) return defaultOW;
         if (paletteRun == null) return defaultOW;
         var ow = ReadonlyPixelViewModel.Create(model, graphicsRun, paletteRun, true);
         if (flip) ow = ow.ReflectX();
         return ow;
      }

      public void ClearUnused() {
         element.SetValue(2, 0);
         element.SetValue(12, 0);
      }

      private static readonly Dictionary<int, Point> facingVectors = new() {
         [7] = new(0, -1),
         [8] = new(0, 1),
         [9] = new(-1, 0),
         [10] = new(1, 0),
      };
      public bool ShouldHighlight(int x, int y) {
         if (TrainerType != 0 && facingVectors.TryGetValue(MoveType, out var vector)) {
            var (xx, yy) = (X, Y);
            var range = TrainerRangeOrBerryID;
            if (Math.Sign(y - yy) == vector.Y && Math.Sign(x - xx) == vector.X && Math.Abs(y - yy) <= range && Math.Abs(x - xx) <= range) {
               return true;
            }
         } else {
            if (!MoveType.IsAny(2, 3, 4, 5, 6)) return false;
            if (Math.Abs(x - X) <= RangeX && Math.Abs(y - Y) <= RangeY) return true;
         }
         return false;
      }
   }

   public class WarpEventViewModel : BaseEventViewModel {
      public WarpEventViewModel(ModelArrayElement warpEvent) : base(warpEvent, "warpCount") { }

      public int WarpID {
         get => element.GetValue("warpID") + 1;
         set => element.SetValue("warpID", value - 1);
      }

      #region Bank/Map

      public int Bank {
         get => element.GetValue("bank");
         set {
            element.SetValue("bank", value);
            NotifyPropertyChanged();
            if (!ignoreUpdateBankMap) bankMap = null;
            NotifyPropertyChanged(nameof(BankMap));
            NotifyPropertyChanged(nameof(TargetMapName));
         }
      }

      public int Map {
         get => element.GetValue("map");
         set {
            element.SetValue("map", value);
            NotifyPropertyChanged();
            if (!ignoreUpdateBankMap) bankMap = null;
            NotifyPropertyChanged(nameof(BankMap));
            NotifyPropertyChanged(nameof(TargetMapName));
         }
      }

      private bool ignoreUpdateBankMap;
      private string bankMap;
      public string BankMap {
         get {
            if (bankMap == null) bankMap = $"({Bank}, {Map})";
            return bankMap;
         }
         set {
            bankMap = value;
            var parts = value.Split(new[] { ',', ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return;
            ignoreUpdateBankMap = true;
            if (parts[0].TryParseInt(out int bank)) Bank = bank;
            if (parts[1].TryParseInt(out int map)) Map = map;
            ignoreUpdateBankMap = false;
         }
      }

      public string TargetMapName => BlockMapViewModel.MapIDToText(element.Model, Bank, Map);

      #endregion

      public override void Render(IDataModel model, LayoutModel layout) {
         EventRender = BuildEventRender(UncompressedPaletteColor.Pack(0, 0, 31));
         if (WarpIsOnWarpableBlock(model, layout)) return;
         EventRender = BuildEventRender(UncompressedPaletteColor.Pack(0, 0, 31), true);
      }

      public bool WarpIsOnWarpableBlock(IDataModel model, LayoutModel layout) {
         if (!model.TryGetList("MapAttributeBehaviors", out var list)) return false;

         int primaryBlockCount = model.IsFRLG() ? 640 : 512;
         var cell = layout.BlockMap[X, Y];
         var tile = cell.Tile;
         var blockset = layout.PrimaryBlockset;
         if (tile >= primaryBlockCount) {
            tile -= primaryBlockCount;
            blockset = layout.SecondaryBlockset;
         }

         var behavior = blockset.Attribute(tile).Behavior;
         if (list.Count <= behavior) return false;
         return list[behavior].Contains("Warp") || list[behavior].Contains("Door") || list[behavior].Contains("Stairs");
      }
   }

   public class ScriptEventViewModel : BaseEventViewModel {
      private readonly Action<int> gotoAddress;

      public ScriptEventViewModel(Action<int> gotoAddress, ModelArrayElement scriptEvent) : base(scriptEvent, "scriptCount") { this.gotoAddress = gotoAddress; }

      public int Trigger {
         get => element.GetValue("trigger");
         set => element.SetValue("trigger", value);
      }

      private string triggerHex;
      public string TriggerHex {
         get {
            if (triggerHex != null) return triggerHex;
            return triggerHex = Trigger.ToString("X4");
         }
         set {
            triggerHex = value;
            if (!value.TryParseHex(out int result)) return;
            Trigger = result;
         }
      }

      public int Index {
         get => element.GetValue("index");
         set => element.SetValue("index", value);
      }

      public int ScriptAddress {
         get => element.GetAddress("script");
         set => element.SetAddress("script", value);
      }

      public void GotoScript() => gotoAddress(ScriptAddress);

      private string scriptAddressText;
      public string ScriptAddressText {
         get {
            if (scriptAddressText != null) return scriptAddressText;
            var value = element.GetAddress("script");
            return GetAddressText(value, ref scriptAddressText);
         }
         set {
            SetAddressText(value, ref scriptAddressText, "script");
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(ScriptAddress));
         }
      }

      public override void Render(IDataModel model, LayoutModel layout) {
         EventRender = BuildEventRender(UncompressedPaletteColor.Pack(0, 31, 0));
      }
   }

   public class SignpostEventViewModel : BaseEventViewModel {
      // kind. arg::|h
      // kind = 0/1/2/3/4 => arg is a pointer to an XSE script
      // kind = 5/6/7 => arg is itemID: hiddenItemID. attr|t|quantity:::.|isUnderFoot.
      // kind = 8 => arg is secret base ID, just a 4-byte hex number
      // hidden item IDs are just flags starting at 0x3E8 (1000).

      private readonly Action<int> gotoAddress;

      public event EventHandler<DataMovedEventArgs> DataMoved;

      public SignpostEventViewModel(ModelArrayElement signpostEvent, Action<int> gotoAddress) : base(signpostEvent, "signpostCount") {
         new List<string> {
            "Facing Any",
            "Facing North",
            "Facing South",
            "Facing East",
            "Facing West",
            "Hidden Item (unused 1)",
            "Hidden Item (unused 2)",
            "Hidden Item",
            "Secret Base",
         }.ForEach(KindOptions.Add);

         foreach (var item in signpostEvent.Model.GetOptions(HardcodeTablesModel.ItemsTableName)) {
            ItemOptions.Add(item);
         }

         SetDestinationFormat();

         this.gotoAddress = gotoAddress;
      }

      public void SetDestinationFormat() {
         if (!ShowPointer) return;
         var destinationRun = new XSERun(Pointer, SortedSpan<int>.None);
         var existingRun = element.Model.GetNextRun(destinationRun.Start);
         if (existingRun.Start < destinationRun.Start) return; // don't erase existing runs for this
         if (existingRun.Start == destinationRun.Start && existingRun is not NoInfoRun) return;
         element.Model.ObserveRunWritten(new ModelDelta(), destinationRun); // don't track this change
      }

      public void ClearDestinationFormat() {
         if (!ShowPointer) return;
         var destination = Pointer;
         var run = element.Model.GetNextRun(Pointer);
         if (run.Start != destination || (run.PointerSources != null && run.PointerSources.Count > 0)) return;
         element.Model.ClearFormat(element.Token, destination, 1);
      }

      public ObservableCollection<string> KindOptions { get; } = new();

      public int Kind {
         get => element.TryGetValue("kind", out var value) ? value : -1;
         set {
            ClearDestinationFormat();
            var old = element.GetValue("kind");
            element.SetValue("kind", value);
            var wasPointer = old < 5;
            var isPointer = value < 5;
            NotifyPropertiesChanged(nameof(ShowArg), nameof(ShowPointer), nameof(ShowHiddenItemProperties));
            if (ShowHiddenItemProperties) NotifyPropertyChanged(nameof(ItemID));
            SetDestinationFormat();
            if (wasPointer == isPointer) return;
            element.SetValue("arg", 0);
            argText = null;
            pointerText = null;
            NotifyPropertiesChanged(nameof(ArgText), nameof(PointerText), nameof(ShowSignpostText), nameof(ItemID), nameof(HiddenItemID), nameof(Quantity), nameof(CanGotoScript));
         }
      }

      public bool ShowArg => Kind == 8;

      string argText;
      public string ArgText {
         get {
            if (argText != null) return argText;
            argText = element.GetValue("arg").ToString("X8");
            return argText;
         }
         set {
            argText = value;
            if (value.TryParseHex(out int result)) element.SetValue("arg", result);
         }
      }

      #region Show as Pointer

      public bool ShowPointer => Kind < 5;

      public int Pointer {
         get => element.GetAddress("arg");
         set {
            ClearDestinationFormat();
            element.SetAddress("arg", value);
            pointerText = argText = null;
            NotifyPropertiesChanged(nameof(PointerText), nameof(ArgText), nameof(CanGotoScript));
            SetDestinationFormat();
         }
      }

      private string pointerText;
      public string PointerText {
         get {
            if (pointerText != null) return pointerText;
            var value = element.GetAddress("arg");
            return GetAddressText(value, ref pointerText);
         }
         set {
            ClearDestinationFormat();
            SetAddressText(value, ref pointerText, "arg");
            SetDestinationFormat();
            NotifyPropertyChanged(nameof(PointerText), nameof(CanGotoScript));
         }
      }

      public bool CanGotoScript => 0 <= Pointer && Pointer < element.Model.Count;
      public void GotoScript() {
         SetDestinationFormat();
         gotoAddress(Pointer);
      }

      #endregion

      #region Item Properties

      public bool ShowHiddenItemProperties => Kind >= 5 && Kind <= 7;

      // itemID: hiddenItemID. attr|t|quantity:::.|isUnderFoot.
      // arg is at offset '8' of the element

      public ObservableCollection<string> ItemOptions { get; } = new();

      public int ItemID {
         get => element.Model.ReadMultiByteValue(element.Start + 8, 2);
         set {
            element.Model.WriteMultiByteValue(element.Start + 8, 2, element.Token, value);
            NotifyPropertyChanged(nameof(ItemID));
         }
      }

      public byte HiddenItemID {
         get => element.Model[element.Start + 10];
         set => element.Token.ChangeData(element.Model, element.Start + 10, value);
      }

      public byte Quantity {
         get => (byte)(element.Model[element.Start + 11] & 0x7F);
         set {
            var newValue = (byte)((int)value).LimitToRange(0, 0x7F);
            var previous = element.Model[element.Start + 11];
            newValue |= (byte)(previous & 0x80);
            Token.ChangeData(element.Model, element.Start + 11, newValue);
            NotifyPropertyChanged(nameof(Quantity));
         }
      }

      public bool IsUnderFoot {
         get => element.Model[element.Start + 11] >= 0x80;
         set {
            byte newValue = value ? (byte)0x80 : (byte)0;
            var previous = element.Model[element.Start + 11];
            newValue |= (byte)(previous & 0x7F);
            Token.ChangeData(element.Model, element.Start + 11, newValue);
            NotifyPropertyChanged(nameof(IsUnderFoot));
         }
      }

      #endregion

      public override void Render(IDataModel model, LayoutModel layout) {
         EventRender = BuildEventRender(UncompressedPaletteColor.Pack(31, 0, 0));
      }

      public bool ShowSignpostText => EventTemplate.GetSignpostTextPointer(element.Model, this) != DataFormats.Pointer.NULL;

      private string signpostText;
      public string SignpostText {
         get {
            if (signpostText != null) return signpostText;
            signpostText = GetText(EventTemplate.GetSignpostTextPointer(element.Model, this));
            return signpostText;
         }
         set {
            signpostText = value;
            var newAddress = SetText(EventTemplate.GetSignpostTextPointer(element.Model, this), value);
            if (newAddress >= 0) DataMoved.Raise(this, new("Text", newAddress));
         }
      }
   }
}
