using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Xml;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace BlockCount
{
    public class BlockCountModSystem : ModSystem
    {

        ICoreServerAPI api;

        BlockPos start, end;

        string exportPath;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return base.ShouldLoad(forSide);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {

            base.StartServerSide(api);

            var parsers = api.ChatCommands.Parsers;

            exportPath = api.GetOrCreateDataPath("Recipes");

            api.ChatCommands.Create("bc")
            .WithDescription("Counts all unique blocks and their number in a region.\nbc start: register starting block.\nbc stop: register ending block.\nbc count: return list of all blocks in defined region.")
            .RequiresPrivilege(Privilege.chat)
            .RequiresPlayer()
            .BeginSub("start")
                .WithDesc("Set the start point of block count")
                .HandleWith((args) =>
                    {
                        start = args.Caller.Player.Entity.ServerPos.AsBlockPos;
                        return TextCommandResult.Success(String.Format("Start Point Set! At {0},{1},{2}", start.X, start.Y, start.Z));
                    })
            .EndSub()

            .BeginSub("end")
                .WithDesc("Set the end point of block count")
                .HandleWith((args) =>
                {
                    end = args.Caller.Player.Entity.ServerPos.AsBlockPos;
                    return TextCommandResult.Success(String.Format("End Point Set! At {0},{1},{2}", end.X, end.Y, start.Z));
                })
            .EndSub()

            .BeginSub("count")
                .WithDesc("Count the blocks and output it in a nice format. Requires a filename as an argument to be saved to Recipes.")
                .WithArgs(parsers.Word("filename"))
                .HandleWith((args) =>
                {
                    if (start == null)
                    {
                        return TextCommandResult.Error("Please set a start point first.");
                    }
                    if (end == null)
                    {
                        return TextCommandResult.Error("Please set an end point first.");
                    }
                    if (args.Parsers[0].IsMissing)
                    {
                        return TextCommandResult.Error("Please provide a filename");
                    }

                    return CountBlocks(start, end, (string)args.Parsers[0].GetValue(), api);
                    //return TextCommandResult.Success("Debug Message.");
                })
            .EndSub();
        }

        public TextCommandResult CountBlocks(BlockPos start, BlockPos end, string filename, ICoreServerAPI api)
        {
            IBlockAccessor blockAccess = api.World.GetBlockAccessorBulkUpdate(false, false, false);

            Dictionary<string, int> blocks = new();

            BlockPos startPos = new BlockPos(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z));
            BlockPos endPos = new BlockPos(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y), Math.Max(start.Z, end.Z));

            for (int x = startPos.X; x <= endPos.X; x++)
            {
                for (int y = startPos.Y; y <= endPos.Y; y++)
                {
                    for (int z = startPos.Z; z <= endPos.Z; z++)
                    {
                        BlockPos pos = new(x, y, z);
                        string blockId = blockAccess.GetBlock(pos).Code.ToString();
                        if (blockId.Equals("game:air"))
                        {
                            continue;
                        }
                        Debug.WriteLine(blockId);
                        if (blocks.ContainsKey(blockId))
                        {
                            blocks[blockId]++;
                        }
                        else
                        {
                            blocks.Add(blockId, 1);
                        }
                    }
                }
            }

            string output = "";

            foreach (string blockId in blocks.Keys)
            {
                output += "" + String.Format("{0}: {1} instances\n", blockId, blocks[blockId]);
            }

            Debug.WriteLine(output);

            string outfilepath = Path.Combine(exportPath, filename);

            if (!outfilepath.EndsWith(".txt"))
            {
                outfilepath += ".txt";
            }

            try
            {
                using TextWriter textWriter = new StreamWriter(outfilepath);
                textWriter.Write(output);
                textWriter.Close();
            }
            catch (IOException e)
            {
                return TextCommandResult.Success("Failed exporting: " + e.Message);
            }

            return TextCommandResult.Success("Yes this definitely still works.\n" + output, null);
        }






    }
}
