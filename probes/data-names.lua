-- A computer's name on W1-9's map with a computer (D140: custom forces with fixed player settings), its war3map.w3i
-- name "TRIGSTR_001", string 1 "The Horde" (DataProbeMaps.Names).
for i = 0, 2 do
    out("slot " .. i .. " " .. tostring(GetPlayerName(Player(i))))
end
done()
