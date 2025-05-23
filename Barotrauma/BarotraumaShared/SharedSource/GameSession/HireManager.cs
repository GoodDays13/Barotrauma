﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Barotrauma
{
    class HireManager
    {
        public List<CharacterInfo> AvailableCharacters { get; set; }
        public List<CharacterInfo> PendingHires = new List<CharacterInfo>();

        public const int MaxAvailableCharacters = 6;

        public HireManager()
        {
            AvailableCharacters = new List<CharacterInfo>();
        }

        public void RemoveCharacter(CharacterInfo character)
        {
            AvailableCharacters.Remove(character);
        }

        public static int GetSalaryFor(IReadOnlyCollection<CharacterInfo> hires)
        {
            return hires.Sum(hire => GetSalaryFor(hire));
        }

        public static int GetSalaryFor(CharacterInfo hire)
        {
            IEnumerable<Character> crew = GameSession.GetSessionCrewCharacters(CharacterType.Both);
            float multiplier = 0;
            foreach (var character in crew)
            {
                multiplier += character?.Info?.GetSavedStatValueWithAll(StatTypes.HireCostMultiplier, hire.Job.Prefab.Identifier) ?? 0;
            }
            float finalMultiplier = 1f + MathF.Max(multiplier, -1f);
            return (int)(hire.Salary * finalMultiplier);
        }

        public void GenerateCharacters(Location location, int amount)
        {
            AvailableCharacters.ForEach(c => c.Remove());
            AvailableCharacters.Clear();

            foreach (var missingJob in location.Type.GetHireablesMissingFromCrew())
            {
                AddCharacter(missingJob);
                amount--;
            }
            for (int i = 0; i < amount; i++)
            {
                AddCharacter(location.Type.GetRandomHireable());
            }
            if (location.Faction != null) { GenerateFactionCharacters(location.Faction.Prefab); }
            if (location.SecondaryFaction != null) { GenerateFactionCharacters(location.SecondaryFaction.Prefab); }

            void AddCharacter(JobPrefab job)
            {
                if (job == null) { return; }
                //no need for synced rand, these only generate ones and are then included in the campaign save
                int variant = Rand.Range(0, job.Variants, Rand.RandSync.Unsynced);
                AvailableCharacters.Add(new CharacterInfo(CharacterPrefab.HumanSpeciesName, jobOrJobPrefab: job, variant: variant));
            }
        }

        private void GenerateFactionCharacters(FactionPrefab faction)
        {
            foreach (var character in faction.HireableCharacters)
            {
                HumanPrefab humanPrefab = NPCSet.Get(character.NPCSetIdentifier, character.NPCIdentifier);
                if (humanPrefab == null)
                {
                    DebugConsole.ThrowError($"Couldn't create a hireable for the location: character prefab \"{character.NPCIdentifier}\" not found in the NPC set \"{character.NPCSetIdentifier}\".");
                    continue;
                }
                //no need for synced rand, these only generate ones and are then included in the campaign save
                var characterInfo = humanPrefab.CreateCharacterInfo(Rand.RandSync.Unsynced);
                characterInfo.MinReputationToHire = (faction.Identifier, character.MinReputation);
                AvailableCharacters.Add(characterInfo);
            }
        }

        public void Remove()
        {
            AvailableCharacters.ForEach(c => c.Remove());
            AvailableCharacters.Clear();
        }

        public void RenameCharacter(CharacterInfo characterInfo, string newName)
        {
            if (characterInfo == null || string.IsNullOrEmpty(newName)) { return; }
            AvailableCharacters.FirstOrDefault(ci => ci == characterInfo)?.Rename(newName);
            PendingHires.FirstOrDefault(ci => ci == characterInfo)?.Rename(newName);
        }
    }
}
