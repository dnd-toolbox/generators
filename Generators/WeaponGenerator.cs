using System.Net.Http.Json;

namespace generators.Generators {
    
    public class WeaponGenerator {

        private HttpClient _client;
        public WeaponGenerator(HttpClient client) {
            _client = client;
        }
        public async Task<WeaponModel> GenerateWeapon() {
            var rarity = GetRandomRarity();
            return await GenerateWeapon(rarity);
        }

        private Rarity GetRandomRarity() {
            var rollForRarity = Random.Shared.NextSingle()*100;
            switch(rollForRarity) {
                case > 75:
                    return Rarity.VeryRare;
                case > 50:
                    return Rarity.VeryRare;
                case > 25:
                    return Rarity.Uncommon;
                default:
                    return Rarity.Common;
            }
        }

        private async Task<WeaponModel> GenerateWeapon(Rarity rarity) {
            var data = await GetRandomWeaponRecord(rarity);
            var name = data.enhancement.isAdjective ? $"{data.enhancement.name} {data.weaponType.name}" : $"{data.weaponType.name} {data.enhancement.name}";
            var description = string.Format(data.enhancement.description, data.weaponType.name);
            var damage = GetWeaponDamage(data.weaponType.damage, data.enhancement.modifiers);
            return new WeaponModel(
                name,
                rarity,
                description,
                weaponType: data.weaponType.name,
                weaponCategory: data.weaponType.category,
                toHitBonus: $"+{(int) rarity / 2}",
                damage,
                cost: data.weaponType.baseCost*Math.Pow(10, (int)rarity),
                weight: data.weaponType.weight,
                properties: data.weaponType.properties.Union(data.enhancement.properties).ToArray()
            );
        }

        private async Task<WeaponRecord> GetRandomWeaponRecord(Rarity rarity) {
            var path = "sample-data/weapon.json";
            var data = await _client.GetFromJsonAsync<WeaponData>(path) ?? new WeaponData(new List<Enhancement>(), new List<WeaponType>());
            var filteredData = GetDataWithRarity(data, rarity);
            return GetRandomWeaponData(filteredData);
        }

        private WeaponData GetDataWithRarity(WeaponData data, Rarity rarity) {
            return new WeaponData(
                enhancements: data.enhancements.Where(a => a.rarity == rarity),
                weaponTypes: data.weaponTypes
            );
        }
        private WeaponRecord GetRandomWeaponData( WeaponData data) {
            return new WeaponRecord(
                enhancement: data.enhancements.OrderBy(a => Guid.NewGuid()).First(),
                weaponType: data.weaponTypes.OrderBy(a => Guid.NewGuid()).First()
            );
        }

        private string GetWeaponDamage(Damage baseDamage, IEnumerable<Modifier> modifiers) {
            var damages = modifiers.Aggregate(baseDamage, ApplyModifiers);
            var damageDiceStrings = damages.dice.Where(d => d.diceCount != 0).Select(d => $"{d.diceCount}d{d.diceSize} ({d.damageType})");
            var fixedDamageStrings = damages.fixedDamage.Where(d => d.amount != 0).Select(d => $"{d.amount} ({d.damageType})");
            return string.Join(" + ", damageDiceStrings.Union(fixedDamageStrings));
        }

        private Damage ApplyModifiers(Damage damage, Modifier modifier) {
            var newDamage = damage;
            switch(modifier.type){
                case "AllDamageOfType":
                    return SetDamageType(damage, modifier);
                case "BonusDamageOfType":
                    return AddDamageOfType(damage, modifier);
                case "DiceModifier":
                    return ModifyValues(damage, modifier);
            }

            return damage;
        }

        private Damage ModifyValues(Damage damage, Modifier mod) {
            var selection = mod.values.DefaultIfEmpty("None").Skip(0).FirstOrDefault();
            var operation = mod.values.DefaultIfEmpty("None").Skip(1).FirstOrDefault();
            int.TryParse(mod.values.DefaultIfEmpty("0").Skip(2).FirstOrDefault(), out int intensity);

            switch (selection, operation) {
                case ("All", "Replace"):
                    return new Damage(dice: damage.dice.Select(d => new DamageDice(d.diceCount, intensity, d.damageType)), fixedDamage: damage.fixedDamage);
                case ("First", "Replace"):
                    return new Damage(dice: damage.dice.Select((d, i) => i > 0 ? d : new DamageDice(d.diceCount, intensity, d.damageType)), fixedDamage: damage.fixedDamage);
                case ("Last", "Replace"):
                        return new Damage(dice: damage.dice.Select((d, i) => i + 1 < damage.dice.Count() ? d : new DamageDice(d.diceCount, intensity, d.damageType)), fixedDamage: damage.fixedDamage);
                case ("All", "Grow"):
                    return new Damage(dice: damage.dice.Select(d => new DamageDice(d.diceCount, ModifyDiceSize(d.diceSize, intensity), d.damageType)), fixedDamage: damage.fixedDamage);
                case ("First", "Grow"):
                    return new Damage(dice: damage.dice.Select((d, i) => i > 0 ? d : new DamageDice(d.diceCount, ModifyDiceSize(d.diceSize, intensity), d.damageType)), fixedDamage: damage.fixedDamage);
                case ("Last", "Grow"):
                    return new Damage(dice: damage.dice.Select((d, i) => i + 1 < damage.dice.Count() ? d : new DamageDice(d.diceCount, ModifyDiceSize(d.diceSize, intensity), d.damageType)), fixedDamage: damage.fixedDamage);
                case ("All", "Remove"):
                    return new Damage(dice: damage.dice.Where(d => false), fixedDamage: damage.fixedDamage);
                case ("First", "Remove"):
                    return new Damage(dice: damage.dice.Skip(1), fixedDamage: damage.fixedDamage);
                case ("Last", "Remove"):
                    return new Damage(dice: damage.dice.Take(damage.dice.Count() - 1), fixedDamage: damage.fixedDamage);
                case ("All", "MultiplyAll"):
                    return new Damage(dice: damage.dice.Select(d => new DamageDice(d.diceCount*intensity, d.diceSize, d.damageType)), fixedDamage: damage.fixedDamage.Select(d => new FixedDamage(d.amount*intensity, d.damageType)));
                case ("All", "MultiplyDice"):
                    return new Damage(dice: damage.dice.Select(d => new DamageDice(d.diceCount*intensity, d.diceSize, d.damageType)), fixedDamage: damage.fixedDamage);
                case ("First", "MultiplyDice"):
                    return new Damage(dice: damage.dice.Select((d, i) => i > 0 ? d : new DamageDice(d.diceCount*intensity, d.diceSize, d.damageType)), fixedDamage: damage.fixedDamage);
                case ("Last", "MultiplyDice"):
                    return new Damage(dice: damage.dice.Select((d, i) => i + 1 < damage.dice.Count() ? d : new DamageDice(d.diceCount*intensity, d.diceSize, d.damageType)), fixedDamage: damage.fixedDamage);
                default: return damage;
            }
        }
        private Damage AddDamageOfType(Damage damage, Modifier mod) {
            int.TryParse(mod.values.DefaultIfEmpty("0").Skip(0).FirstOrDefault(), out int dCount);
            int.TryParse(mod.values.DefaultIfEmpty("0").Skip(1).FirstOrDefault(), out int dSize);
            int.TryParse(mod.values.DefaultIfEmpty("0").Skip(2).FirstOrDefault(), out int fixedD);
            var type = mod.values.DefaultIfEmpty("Unknown").Skip(3).FirstOrDefault();
            
            var newDamage = new Damage(
                dice: damage.dice.Append(new DamageDice(dCount, dSize, type)),
                fixedDamage: damage.fixedDamage.Append(new FixedDamage(fixedD, type))
            );
            return newDamage;
        }

        private Damage SetDamageType(Damage damage, Modifier mod) {
            var type = mod.values.DefaultIfEmpty("Unknown").FirstOrDefault();
            return new Damage(
                dice: damage.dice.Select(d => new DamageDice(d.diceCount, d.diceSize, type)).ToArray(),
                fixedDamage: damage.fixedDamage.Select(d => new FixedDamage(d.amount, type)).ToArray()
            );
        }

        private int ModifyDiceSize(int diceSize, int modifier) {
            var newDiceSize = diceSize;
            while (modifier != 0) {
                var isIncrease = modifier > 0;
                switch(diceSize, isIncrease) {
                    case (4, true): return ModifyDiceSize(6, modifier-1); 
                    case (6, true): return ModifyDiceSize(8, modifier-1); 
                    case (8, true): return ModifyDiceSize(10, modifier-1); 
                    case (10, true): return ModifyDiceSize(12, modifier-1); 
                    case (12, true): return ModifyDiceSize(20, modifier-1); 
                    case (20, true): return ModifyDiceSize(20, modifier-1); // largest dice. 
                    case (4, false): return ModifyDiceSize(4, modifier+1); //smallest dice.
                    case (6, false): return ModifyDiceSize(4, modifier+1); 
                    case (8, false): return ModifyDiceSize(6, modifier+1); 
                    case (10, false): return ModifyDiceSize(8, modifier+1); 
                    case (12, false): return ModifyDiceSize(10, modifier+1); 
                    case (20, false): return ModifyDiceSize(12, modifier+1); 
                }
            }
            return newDiceSize;
        }

        private record Damage(IEnumerable<DamageDice> dice, IEnumerable<FixedDamage> fixedDamage);
        private record FixedDamage(int amount, string damageType);
        private record DamageDice(int diceCount, int diceSize, string damageType);
        private record WeaponRecord(Enhancement enhancement, WeaponType weaponType);
        private record WeaponData(IEnumerable<Enhancement> enhancements, IEnumerable<WeaponType> weaponTypes);
        private record Modifier(string type, string[] values);
        private record Enhancement(string name, string description, bool isAdjective, Modifier[] modifiers, string[] properties, Rarity rarity);
        private record WeaponType(string name, string description, string category, Damage damage, double baseCost, double weight, string[] properties);
        private int[] diceSizes = {0, 2, 4, 6, 8, 12, 20};
       
    }
    
     public enum Rarity {
            Common = 1,
            Uncommon = 2,
            Rare = 3,
            VeryRare = 4,
        }
    public record WeaponModel(string name, Rarity rarity, string description, string weaponType, string weaponCategory, string toHitBonus, string damage, double cost, double weight, string[] properties);

}