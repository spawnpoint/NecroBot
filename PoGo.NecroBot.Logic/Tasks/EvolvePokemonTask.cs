﻿#region using directives

using System.Linq;
using System.Threading;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;
using PokemonGo.RocketAPI;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class EvolvePokemonTask
    {
        public static void Execute(Context ctx, StateMachine machine)
        {
            if (ctx.LogicSettings.UseLuckyEggsWhileEvolving)
            {
                UseLuckyEgg(ctx.Client, ctx.Inventory, machine);
            }

            var pokemonToEvolveTask = ctx.Inventory.GetPokemonToEvolve(ctx.LogicSettings.PokemonsToEvolve);
            pokemonToEvolveTask.Wait();

            var pokemonToEvolve = pokemonToEvolveTask.Result;
            foreach (var pokemon in pokemonToEvolve)
            {
                var evolveTask = ctx.Client.Inventory.EvolvePokemon(pokemon.Id);
                evolveTask.Wait();

                var evolvePokemonOutProto = evolveTask.Result;

                machine.Fire(new PokemonEvolveEvent
                {
                    Id = pokemon.PokemonId,
                    Exp = evolvePokemonOutProto.ExperienceAwarded,
                    Result = evolvePokemonOutProto.Result
                });

                Thread.Sleep(3000);
            }
        }

        public static void UseLuckyEgg(Client client, Inventory inventory, StateMachine machine)
        {
            var inventoryContent = inventory.GetItems().Result;

            var luckyEggs = inventoryContent.Where(p => p.ItemId == ItemId.ItemLuckyEgg);
            var luckyEgg = luckyEggs.FirstOrDefault();

            if (luckyEgg == null || luckyEgg.Count <= 0)
                return;

            client.Inventory.UseItemXpBoost().Wait();

            luckyEgg.Count -= 1;
            machine.Fire(new UseLuckyEggEvent {Count = luckyEgg.Count});

            Thread.Sleep(2000);
        }
    }
}