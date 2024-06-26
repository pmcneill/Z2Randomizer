﻿using Assembler;
using Microsoft.ClearScript;
using NLog;
using RandomizerCore;
using RandomizerCore.Sidescroll;
using SD.Tools.Algorithmia.GeneralDataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using static Z2Randomizer.Core.Util;

namespace Z2Randomizer.Core.Sidescroll;

public class Palaces
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private const int PALACE_SHUFFLE_ATTEMPT_LIMIT = 100;
    private const int DROP_PLACEMENT_FAILURE_LIMIT = 100;
    private const int ROOM_PLACEMENT_FAILURE_LIMIT = 200;

    private static readonly RequirementType[] VANILLA_P1_ALLOWED_BLOCKERS = [ 
        RequirementType.KEY ];
    private static readonly RequirementType[] VANILLA_P2_ALLOWED_BLOCKERS = [ 
        RequirementType.KEY, RequirementType.JUMP, RequirementType.GLOVE ];
    private static readonly RequirementType[] VANILLA_P3_ALLOWED_BLOCKERS = [ 
        RequirementType.KEY, RequirementType.DOWNSTAB, RequirementType.UPSTAB, RequirementType.GLOVE];
    private static readonly RequirementType[] VANILLA_P4_ALLOWED_BLOCKERS = [ 
        RequirementType.KEY, RequirementType.FAIRY, RequirementType.JUMP];
    private static readonly RequirementType[] VANILLA_P5_ALLOWED_BLOCKERS = [ 
        RequirementType.KEY, RequirementType.FAIRY, RequirementType.JUMP];
    private static readonly RequirementType[] VANILLA_P6_ALLOWED_BLOCKERS = [ 
        RequirementType.KEY, RequirementType.FAIRY, RequirementType.JUMP, RequirementType.GLOVE];
    private static readonly RequirementType[] VANILLA_P7_ALLOWED_BLOCKERS = [ 
        RequirementType.FAIRY, RequirementType.UPSTAB, RequirementType.DOWNSTAB, RequirementType.JUMP, RequirementType.GLOVE];

    public static readonly RequirementType[][] ALLOWED_BLOCKERS_BY_PALACE = [ 
        VANILLA_P1_ALLOWED_BLOCKERS,
        VANILLA_P2_ALLOWED_BLOCKERS,
        VANILLA_P3_ALLOWED_BLOCKERS,
        VANILLA_P4_ALLOWED_BLOCKERS,
        VANILLA_P5_ALLOWED_BLOCKERS,
        VANILLA_P6_ALLOWED_BLOCKERS,
        VANILLA_P7_ALLOWED_BLOCKERS
    ];


    private static readonly SortedDictionary<int, int> palaceConnectionLocs = new SortedDictionary<int, int>
    {
        {1, 0x1072B},
        {2, 0x1072B},
        {3, 0x12208},
        {4, 0x12208},
        {5, 0x1072B},
        {6, 0x12208},
        {7, 0x1472B},
    };

    private static readonly Dictionary<int, int> palaceAddr = new Dictionary<int, int>
    {
        {1, 0x4663 },
        {2, 0x4664 },
        {3, 0x4665 },
        {4, 0xA140 },
        {5, 0x8663 },
        {6, 0x8664 },
        {7, 0x8665 }
    };

    public static List<Palace> CreatePalaces(BackgroundWorker worker, Random r, RandomizerProperties props, bool raftIsRequired)
    {
        if (props.UseCustomRooms && !File.Exists("CustomRooms.json"))
        {
            throw new Exception("Couldn't find CustomRooms.json. Please create the file or disable custom rooms on the misc tab.");
        }
        List<Palace> palaces = new List<Palace>();
        List<Room> roomPool = new List<Room>();
        List<Room> gpRoomPool = new List<Room>();
        //Dictionary<byte[], List<Room>> sideviews = new Dictionary<byte[], List<Room>>(new Util.StandardByteArrayEqualityComparer());
        //Dictionary<byte[], List<Room>> sideviewsgp = new Dictionary<byte[], List<Room>>(new Util.StandardByteArrayEqualityComparer());
        int mapNo = 0;
        int mapNoGp = 0;
        //This is unfortunate because there is no list-backed MutliValueDictionary like Apache has, so we have to deal with
        //O(n) random access.
        MultiValueDictionary<int, Room> entrancesByPalaceNumber = new();
        MultiValueDictionary<int, Room> bossRoomsByPalaceNumber = new();
        List<Room> tbirdRooms = new();
        MultiValueDictionary<Direction, Room> itemRoomsByDirection = new();
        if (props.AllowVanillaRooms)
        {
            for (int palaceNum = 1; palaceNum < 8; palaceNum++)
            {
                entrancesByPalaceNumber.AddRange(palaceNum, PalaceRooms.Entrances(RoomGroup.VANILLA, props.UseCustomRooms)
                    .Where(i => i.PalaceNumber == null || i.PalaceNumber == palaceNum).ToList());
                bossRoomsByPalaceNumber.AddRange(palaceNum, PalaceRooms.BossRooms(RoomGroup.VANILLA, props.UseCustomRooms)
                    .Where(i => (i.PalaceNumber == null && palaceNum < 6) || i.PalaceNumber == palaceNum).ToList());
                tbirdRooms.AddRange(PalaceRooms.TBirdRooms(RoomGroup.VANILLA, props.UseCustomRooms)
                    .Where(i => i.PalaceNumber == null || i.PalaceNumber == palaceNum).ToList());
                foreach (Direction direction in DirectionExtensions.ITEM_ROOM_ORIENTATIONS)
                {
                    itemRoomsByDirection.AddRange(direction, PalaceRooms.ItemRoomsByDirection(RoomGroup.VANILLA, direction, props.UseCustomRooms).ToList());
                }
            }  
        }

        if (props.AllowV4Rooms)
        {
            for (int palaceNum = 1; palaceNum < 8; palaceNum++)
            {
                entrancesByPalaceNumber.AddRange(palaceNum, PalaceRooms.Entrances(RoomGroup.V4_0, props.UseCustomRooms)
                    .Where(i => i.PalaceNumber == null || i.PalaceNumber == palaceNum).ToList());
                bossRoomsByPalaceNumber.AddRange(palaceNum, PalaceRooms.BossRooms(RoomGroup.V4_0, props.UseCustomRooms)
                    .Where(i => (i.PalaceNumber == null && palaceNum < 6) || i.PalaceNumber == palaceNum).ToList());
                tbirdRooms.AddRange(PalaceRooms.TBirdRooms(RoomGroup.V4_0, props.UseCustomRooms)
                    .Where(i => i.PalaceNumber == null || i.PalaceNumber == palaceNum).ToList());
                foreach (Direction direction in DirectionExtensions.ITEM_ROOM_ORIENTATIONS)
                {
                    itemRoomsByDirection.AddRange(direction, PalaceRooms.ItemRoomsByDirection(RoomGroup.V4_0, direction, props.UseCustomRooms).ToList());
                }
            }
        }

        if (props.AllowV4_4Rooms)
        {
            for(int palaceNum = 1; palaceNum < 8; palaceNum++)
            {
                entrancesByPalaceNumber.AddRange(palaceNum, PalaceRooms.Entrances(RoomGroup.V4_4, props.UseCustomRooms)
                    .Where(i => i.PalaceNumber == null || i.PalaceNumber == palaceNum).ToList());
                bossRoomsByPalaceNumber.AddRange(palaceNum, PalaceRooms.BossRooms(RoomGroup.V4_4, props.UseCustomRooms)
                    .Where(i => (i.PalaceNumber == null && palaceNum < 6) || i.PalaceNumber == palaceNum).ToList());
                tbirdRooms.AddRange(PalaceRooms.TBirdRooms(RoomGroup.V4_4, props.UseCustomRooms)
                    .Where(i => i.PalaceNumber == null || i.PalaceNumber == palaceNum).ToList());
                foreach (Direction direction in DirectionExtensions.ITEM_ROOM_ORIENTATIONS)
                {
                    itemRoomsByDirection.AddRange(direction, PalaceRooms.ItemRoomsByDirection(RoomGroup.V4_4, direction, props.UseCustomRooms).ToList());
                }
            }           
        }

        //If we're using a room set that has no entraces or boss rooms, we still need to have something, so add the vanilla ones.
        for (int palaceNum = 1; palaceNum < 8; palaceNum++)
        {
            if (!entrancesByPalaceNumber.ContainsKey(palaceNum) || entrancesByPalaceNumber[palaceNum].Count == 0)
            {
                entrancesByPalaceNumber.AddRange(palaceNum, PalaceRooms.Entrances(RoomGroup.VANILLA, props.UseCustomRooms)
                    .Where(i => i.PalaceNumber == palaceNum).ToList());
            }
            if (!bossRoomsByPalaceNumber.ContainsKey(palaceNum) || bossRoomsByPalaceNumber[palaceNum].Count == 0)
            {
                bossRoomsByPalaceNumber.Add(palaceNum, PalaceRooms.VanillaBossRoom(palaceNum));
            }
        }

        if(tbirdRooms.Count == 0)
        {
            tbirdRooms.Add(PalaceRooms.TBirdRooms(RoomGroup.VANILLA, props.UseCustomRooms).First());
        }

        if (props.NormalPalaceStyle.IsReconstructed())
        {
            roomPool.Clear();
            if (props.AllowVanillaRooms)
            {
                roomPool.AddRange(PalaceRooms.NormalPalaceRoomsByGroup(RoomGroup.VANILLA, props.UseCustomRooms));
            }

            if (props.AllowV4Rooms)
            {
                roomPool.AddRange(PalaceRooms.NormalPalaceRoomsByGroup(RoomGroup.V4_0, props.UseCustomRooms));
            }

            if (props.AllowV4_4Rooms)
            {
                roomPool.AddRange(PalaceRooms.NormalPalaceRoomsByGroup(RoomGroup.V4_4, props.UseCustomRooms));
            }
        }


        if (props.GPStyle.IsReconstructed())
        {
            gpRoomPool.Clear();
            if (props.AllowVanillaRooms)
            {
                gpRoomPool.AddRange(PalaceRooms.GPRoomsByGroup(RoomGroup.VANILLA, props.UseCustomRooms));
            }

            if (props.AllowV4Rooms)
            {
                gpRoomPool.AddRange(PalaceRooms.GPRoomsByGroup(RoomGroup.V4_0, props.UseCustomRooms));
            }

            if (props.AllowV4_4Rooms)
            {
                gpRoomPool.AddRange(PalaceRooms.GPRoomsByGroup(RoomGroup.V4_4, props.UseCustomRooms));
            }
        }


        int[] sizes = new int[7];

        sizes[0] = r.Next(10, 17);
        sizes[1] = r.Next(16, 25);
        sizes[2] = r.Next(11, 18);
        sizes[3] = r.Next(16, 25);
        sizes[4] = r.Next(23, 63 - sizes[0] - sizes[1]);
        sizes[5] = r.Next(22, 63 - sizes[2] - sizes[3]);

        if (props.GPStyle == PalaceStyle.RECONSTRUCTED_SHORTENED)
        {
            sizes[6] = r.Next(27, 41);
        }
        else
        {
            sizes[6] = r.Next(54, 60);
        }

        for (int currentPalace = 1; currentPalace < 8; currentPalace++)
        {
            Palace palace;
            if (currentPalace == 7 && props.GPStyle.IsReconstructed() || currentPalace < 7 && props.NormalPalaceStyle.IsReconstructed())
            {
                int tries = 0;
                int innertries = 0;
                int palaceGroup = currentPalace switch
                {
                    1 => 1,
                    2 => 1,
                    3 => 2,
                    4 => 2,
                    5 => 1,
                    6 => 2,
                    7 => 3,
                    _ => throw new ImpossibleException("Invalid palace number: " + currentPalace)
                };

                do // while (tries >= PALACE_SHUFFLE_ATTEMPT_LIMIT);
                {
                    if (worker != null && worker.CancellationPending)
                    {
                        return null;
                    }

                    tries = 0;
                    innertries = 0;
                    int roomPlacementFailures = 0;
                    //bool done = false;
                    do //while (roomPlacementFailures > ROOM_PLACEMENT_FAILURE_LIMIT || palace.AllRooms.Any(i => i.CountOpenExits() > 0));

                    {
                        List<Room> palaceRoomPool = new List<Room>(currentPalace == 7 ? gpRoomPool : roomPool)
                            .Where(i => i.PalaceNumber == null || i.PalaceNumber == currentPalace).ToList();
                        mapNo = currentPalace switch
                        {
                            1 => 0,
                            2 => palaces[0].AllRooms.Count,
                            3 => 0,
                            4 => palaces[2].AllRooms.Count,
                            5 => palaces[0].AllRooms.Count + palaces[1].AllRooms.Count,
                            6 => mapNo = palaces[2].AllRooms.Count + palaces[3].AllRooms.Count,
                            _ => 0
                        };

                        if (currentPalace == 7)
                        {
                            mapNoGp = 0;
                        }

                        palace = new Palace(currentPalace, palaceAddr[currentPalace], palaceConnectionLocs[currentPalace], props.UseCustomRooms);
                        palace.Root = new(entrancesByPalaceNumber[currentPalace].ElementAt(r.Next(entrancesByPalaceNumber[currentPalace].Count)))
                        {
                            IsRoot = true,
                            PalaceGroup = palaceGroup
                        };
                        palace.AllRooms.Add(palace.Root);

                        palace.BossRoom = new(bossRoomsByPalaceNumber[currentPalace].ElementAt(r.Next(bossRoomsByPalaceNumber[currentPalace].Count)));
                        palace.BossRoom.Enemies = (byte[])PalaceRooms.VanillaBossRoom(currentPalace).Enemies.Clone();
                        palace.BossRoom.NewEnemies = palace.BossRoom.Enemies;
                        palace.BossRoom.PalaceGroup = palaceGroup;
                        palace.AllRooms.Add(palace.BossRoom);

                        if (currentPalace < 7) //Not GP
                        {
                            Direction itemRoomDirection;
                            Room itemRoom = null;
                            while (itemRoom == null)
                            {
                                itemRoomDirection = DirectionExtensions.RandomItemRoomOrientation(r);
                                if (!itemRoomsByDirection.ContainsKey(itemRoomDirection))
                                {
                                    continue;
                                }
                                itemRoom = new(itemRoomsByDirection[itemRoomDirection].ElementAt(r.Next(itemRoomsByDirection[itemRoomDirection].Count)));
                            }
                            palace.ItemRoom = itemRoom;
                            palace.ItemRoom.PalaceGroup = palaceGroup;
                            palace.AllRooms.Add(palace.ItemRoom);

                            palace.Root.NewMap = mapNo;
                            IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);
                            palace.BossRoom.NewMap = mapNo;
                            if (props.BossRoomConnect)
                            {
                                palace.BossRoom.RightByte = 0x69;
                            }
                            IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);
                            palace.ItemRoom.NewMap = mapNo;
                            palace.ItemRoom.SetItem((Item)currentPalace);
                            IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);

                            if (palace.ItemRoom.LinkedRoomName != null)
                            {
                                Room segmentedItemRoom1, segmentedItemRoom2;
                                segmentedItemRoom1 = palace.ItemRoom;
                                segmentedItemRoom2 = new(PalaceRooms.GetRoomByName(segmentedItemRoom1.LinkedRoomName, props.UseCustomRooms));
                                segmentedItemRoom2.NewMap = palace.ItemRoom.NewMap;
                                segmentedItemRoom2.PalaceGroup = palaceGroup;
                                segmentedItemRoom2.SetItem((Item)currentPalace);
                                segmentedItemRoom2.LinkedRoom = segmentedItemRoom1;
                                segmentedItemRoom1.LinkedRoom = segmentedItemRoom2;
                                palace.AllRooms.Add(segmentedItemRoom2);
                                palace.SetOpenRoom(segmentedItemRoom2);
                            }
                            palace.SetOpenRoom(palace.Root);
                        }
                        else //GP
                        {
                            palace.Root.NewMap = mapNoGp;
                            IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);
                            palace.BossRoom.NewMap = mapNoGp;
                            IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);

                            //thunderbird?
                            if (!props.RemoveTbird)
                            {

                                palace.Tbird = new(tbirdRooms[r.Next(tbirdRooms.Count)]);
                                palace.Tbird.NewMap = mapNoGp;
                                palace.Tbird.PalaceGroup = 3;
                                IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);
                                palace.AllRooms.Add(palace.Tbird);
                            }
                            palace.SetOpenRoom(palace.Root);

                        }


                        palace.MaxRooms = sizes[currentPalace - 1];

                        //add rooms
                        roomPlacementFailures = 0;
                        while (palace.AllRooms.Count < palace.MaxRooms)
                        {
                            if (palaceRoomPool.Count == 0)
                            {
                                throw new Exception("Palace room pool was empty");
                            }
                            int roomIndex = r.Next(palaceRoomPool.Count);
                            Room roomToAdd = new(palaceRoomPool[roomIndex]);

                            roomToAdd.PalaceGroup = palaceGroup;
                            roomToAdd.NewMap = currentPalace < 7 ? mapNo : mapNoGp;
                            bool added = true;
                            if (roomToAdd.HasDrop && !palaceRoomPool.Any(i => i.IsDropZone && i != roomToAdd))
                            {
                                //Debug.WriteLine(palace.AllRooms.Count + " - 0");
                                added = false;
                            }
                            if(props.NoDuplicateRoomsBySideview)
                            {
                                if(palace.AllRooms.Any(i => byteArrayEqualityComparer.Equals(i.SideView, roomToAdd.SideView)))
                                {
                                    Room test = palace.AllRooms.First(i => byteArrayEqualityComparer.Equals(i.SideView, roomToAdd.SideView));
                                    added = false;
                                } 
                            }
                            if(added)
                            {
                                added = palace.AddRoom(roomToAdd, props.BlockersAnywhere);
                                if (added && roomToAdd.LinkedRoomName != null)
                                {
                                    Room linkedRoom = new(PalaceRooms.GetRoomByName(roomToAdd.LinkedRoomName, props.UseCustomRooms));
                                    linkedRoom.NewMap = currentPalace < 7 ? mapNo : mapNoGp;
                                    linkedRoom.LinkedRoom = roomToAdd;
                                    roomToAdd.LinkedRoom = linkedRoom;
                                    palace.AddRoom(linkedRoom, props.BlockersAnywhere);
                                }
                            }
                            if (added)
                            {
                                if (props.NoDuplicateRooms)
                                {
                                    palaceRoomPool.RemoveAt(roomIndex);
                                }
                                IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);
                                if (roomToAdd.LinkedRoom?.HasDrop ?? false)
                                {
                                    roomToAdd = roomToAdd.LinkedRoom;
                                }
                                if (roomToAdd.HasDrop)
                                {
                                    int numDrops = r.Next(Math.Min(3, palace.MaxRooms - palace.AllRooms.Count), Math.Min(6, palace.MaxRooms - palace.AllRooms.Count));
                                    numDrops = Math.Min(numDrops, palaceRoomPool.Count(i => i.IsDropZone) + 1);
                                    bool continueDropping = true;
                                    int j = 0;
                                    int dropPlacementFailures = 0;
                                    while (j < numDrops && continueDropping)
                                    {
                                        List<Room> possibleDropZones = palaceRoomPool.Where(i => i.IsDropZone).ToList();
                                        if (possibleDropZones.Count == 0)
                                        {
                                            logger.Debug("Exhausted all available drop zones");
                                            return null;
                                        }
                                        Room dropZoneRoom = new(possibleDropZones[r.Next(0, possibleDropZones.Count)]);
                                        dropZoneRoom.NewMap = currentPalace < 7 ? mapNo : mapNoGp;
                                        bool added2 = palace.AddRoom(dropZoneRoom, props.BlockersAnywhere);
                                        if (added2)
                                        {
                                            if (props.NoDuplicateRooms)
                                            {
                                                palaceRoomPool.Remove(dropZoneRoom);
                                            }
                                            continueDropping = dropZoneRoom.HasDrop;
                                            if(dropZoneRoom.LinkedRoomName != null)
                                            {
                                                Room linkedRoom = new(PalaceRooms.GetRoomByName(dropZoneRoom.LinkedRoomName, props.UseCustomRooms));
                                                linkedRoom.NewMap = currentPalace < 7 ? mapNo : mapNoGp;
                                                if (palace.AddRoom(linkedRoom, props.BlockersAnywhere))
                                                {
                                                    linkedRoom.LinkedRoom = dropZoneRoom;
                                                    dropZoneRoom.LinkedRoom = linkedRoom;
                                                    //If the drop zone isn't a drop, but is linked to a room that is a drop, keep dropping from the linked room
                                                    if (!continueDropping && linkedRoom.HasDrop)
                                                    {
                                                        continueDropping = true;
                                                    }
                                                }
                                            }
                                            IncrementMapNo(ref mapNo, ref mapNoGp, currentPalace);
                                            j++;
                                        }
                                        else if (++dropPlacementFailures > DROP_PLACEMENT_FAILURE_LIMIT)
                                        {
                                            logger.Trace("Drop placement failure limit exceeded.");
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                roomPlacementFailures ++;
                                if (Hyrule.UNSAFE_DEBUG)
                                {
                                    //Debug.WriteLine("Failed to place room: " + roomToAdd.GetDebuggerDisplay());
                                }
                            }
                            if (roomPlacementFailures >= ROOM_PLACEMENT_FAILURE_LIMIT)
                            {
                                break;
                            }

                            if (palace.GetOpenRooms() >= palace.MaxRooms - palace.AllRooms.Count) //consolidate
                            {
                                palace.Consolidate();
                            }

                        }

                        innertries++;
                    } while (roomPlacementFailures >= ROOM_PLACEMENT_FAILURE_LIMIT
                        || palace.AllRooms.Any(i => i.CountOpenExits() > 0)
                      );

                    if (roomPlacementFailures != ROOM_PLACEMENT_FAILURE_LIMIT)
                    {
                        int count = 0;
                        bool reachable = false;
                        do
                        {
                            palace.ResetRooms();
                            count++;
                            palace.ShuffleRooms(r);
                            reachable = palace.AllReachable();
                            tries++;
                            logger.Debug("Palace room shuffle attempt #" + tries);
                        }
                        while (
                            (!reachable || (currentPalace == 7 && props.RequireTbird && !palace.RequiresThunderbird()) || palace.HasDeadEnd())
                            && (tries < PALACE_SHUFFLE_ATTEMPT_LIMIT)
                            );
                    }
                } while (tries >= PALACE_SHUFFLE_ATTEMPT_LIMIT);
                palace.Generations += tries;
                palaces.Add(palace);
            }
            //NOT RECONSTRUCTED
            else
            {
                palace = new Palace(currentPalace, palaceAddr[currentPalace], palaceConnectionLocs[currentPalace], props.UseCustomRooms);
                PalaceStyle palaceStyle = currentPalace == 7 ? props.GPStyle : props.NormalPalaceStyle;
                //p.dumpMaps();
                int palaceGroup = currentPalace switch
                {
                    1 => 1,
                    2 => 1,
                    3 => 2,
                    4 => 2,
                    5 => 1,
                    6 => 2,
                    7 => 3,
                    _ => throw new ImpossibleException("Invalid palace number: " + currentPalace)
                };

                palace.Root = new(entrancesByPalaceNumber[currentPalace].First());
                palace.Root.PalaceGroup = palaceGroup;
                palace.BossRoom = new(PalaceRooms.VanillaBossRoom(currentPalace));
                palace.BossRoom.PalaceGroup = palaceGroup;
                palace.AllRooms.Add(palace.Root);
                if (currentPalace != 7)
                {
                    Room itemRoom = new(PalaceRooms.VanillaItemRoom(currentPalace));

                    palace.ItemRoom = itemRoom;
                    palace.ItemRoom.PalaceGroup = palaceGroup;
                    palace.AllRooms.Add(palace.ItemRoom);
                }
                palace.AllRooms.Add(palace.BossRoom);
                if (currentPalace == 7)
                {
                    Room bird = new(PalaceRooms.TBirdRooms(RoomGroup.VANILLA, props.UseCustomRooms).First());
                    bird.PalaceGroup = palaceGroup;
                    palace.AllRooms.Add(bird);
                    palace.Tbird = bird;

                }
                foreach (Room v in PalaceRooms.VanillaPalaceRoomsByPalaceNumber(currentPalace, props.UseCustomRooms))
                {
                    Room room = new(v);
                    room.PalaceGroup = palaceGroup;
                    palace.AllRooms.Add(room);
                    
                    if(room.LinkedRoomName != null)
                    {
                        Room linkedRoom = new(PalaceRooms.GetRoomByName(room.LinkedRoomName, props.UseCustomRooms));
                        linkedRoom.PalaceGroup = palaceGroup;
                        linkedRoom.LinkedRoom = room;
                        room.LinkedRoom = linkedRoom;
                        palace.AllRooms.Add(linkedRoom);
                    }
                }
                bool removeTbird = (currentPalace == 7 && props.RemoveTbird);
                palace.CreateTree(removeTbird);

                //I broke shortened vanilla GP. I'm not sure anyone cares.
                /*
                if (currentPalace == 7 && props.ShortenGP)
                {
                    palace.Shorten(r);
                }
                */

                if (palaceStyle == PalaceStyle.SHUFFLED)
                {
                    palace.ShuffleRooms(r);
                }
                while (!palace.AllReachable() || (currentPalace == 7 && props.RequireTbird && !palace.RequiresThunderbird()) || palace.HasDeadEnd())
                {
                    if(palaceStyle == PalaceStyle.VANILLA)
                    {
                        throw new Exception("Vanilla palace (" + currentPalace + ") was not all reachable. This should be impossible.");
                    }
                    palace.ResetRooms();
                    if (palaceStyle == PalaceStyle.SHUFFLED)
                    {
                        palace.ShuffleRooms(r);
                    }
                }
                palaces.Add(palace);
            }
        
        }

        if (!ValidatePalaces(props, raftIsRequired, palaces))
        {
            return null;
        }

        return palaces;
    }

    //This method (and the entire separation of mapNo and mapNoGP) is a digshakeism that should be refactored out
    private static void IncrementMapNo(ref int mapNo, ref int mapNoGp, int i)
    {
        if (i < 7)
        {
            mapNo++;
        }
        else
        {
            mapNoGp++;
        }
    }

    private static bool ValidatePalaces(RandomizerProperties props, bool raftIsRequired, List<Palace> palaces)
    {
        //Enforce aggregate max length of enemy data
        if (palaces.Where(i => i.Number != 7).Sum(i => i.AllRooms.Sum(j => j.Enemies.Length)) > 0x400
            || palaces.Where(i => i.Number == 7).Sum(i => i.AllRooms.Sum(j => j.Enemies.Length)) > 0x2A9)
        {
            return false;
        }
        return CanGetGlove(props, palaces[1])
            && CanGetRaft(props, raftIsRequired, palaces[1], palaces[2])
            && AtLeastOnePalaceCanHaveGlove(props, palaces);
    }
    private static bool AtLeastOnePalaceCanHaveGlove(RandomizerProperties props, List<Palace> palaces)
    {
        List<RequirementType> requireables =
        [
            RequirementType.KEY,
            RequirementType.UPSTAB,
            RequirementType.DOWNSTAB,
            RequirementType.JUMP,
            RequirementType.FAIRY
        ];
        for(int i = 0; i < 6; i++)
        {
            //If there is at least one palace that would be clearable with everything but the glove
            //that palace could contain the glove, so we're not deadlocked.
            if (palaces[i].CanClearAllRooms(requireables, Item.GLOVE))
            {
                return true;
            }
        }
        return false;
    }

    private static bool CanGetGlove(RandomizerProperties props, Palace palace2)
    {
        if (!props.ShufflePalaceItems)
        {
            List<RequirementType> requireables = new List<RequirementType>();
            //If shuffle overworld items is on, we assume you can get all the items / spells
            //as all progression items will eventually shuffle into spots that work
            if (props.ShuffleOverworldItems)
            {
                requireables.Add(RequirementType.KEY);
            }
            //Otherwise if it's vanilla items we can't get the magic key, because we could need glove for boots for flute to get to new kasuto
            requireables.Add(props.SwapUpAndDownStab ? RequirementType.UPSTAB : RequirementType.DOWNSTAB);
            requireables.Add(RequirementType.JUMP);
            requireables.Add(RequirementType.FAIRY);

            //If we can't clear 2 without the items available, we can never get the glove, so the palace is unbeatable
            if (!palace2.CanClearAllRooms(requireables, Item.GLOVE))
            {
                return false;
            }
        }
        return true;
    }

    private static bool CanGetRaft(RandomizerProperties props, bool raftIsRequired, Palace palace2, Palace palace3)
    {
        //if the flagset has vanilla connections and vanilla palace items, you have to be able to get the raft
        //or it will send the logic into an uinrecoverable nosedive since the palaces can't re-generate
        if (!props.ShufflePalaceItems && raftIsRequired)
        {
            List<RequirementType> requireables = new List<RequirementType>();
            //If shuffle overworld items is on, we assume you can get all the items / spells
            //as all progression items will eventually shuffle into spots that work
            if (props.ShuffleOverworldItems)
            {
                requireables.Add(RequirementType.KEY);
                requireables.Add(RequirementType.GLOVE);
                requireables.Add(props.SwapUpAndDownStab ? RequirementType.UPSTAB : RequirementType.DOWNSTAB);
                requireables.Add(RequirementType.JUMP);
                requireables.Add(RequirementType.FAIRY);
            }
            //Otherwise we can only get the things you can get on the west normally
            else
            {
                requireables.Add(RequirementType.JUMP);
                requireables.Add(RequirementType.FAIRY);
                requireables.Add(props.SwapUpAndDownStab ? RequirementType.UPSTAB : RequirementType.DOWNSTAB);
            }
            //If we can clear P2 with this stuff, we can also get the glove
            if (palace2.CanClearAllRooms(requireables, Item.GLOVE))
            {
                requireables.Add(RequirementType.GLOVE);
            }
            //If we can't clear 3 with all the items available on west/DM, we can never raft out, and so we're stuck forever
            //so start over
            if (!palace3.CanClearAllRooms(requireables, Item.RAFT))
            {
                return false;
            }
        }
        return true;
    }

}
