-- R2SW with the widest widths, apart from strings-4 in case the game fails on them: each line is the string's length,
-- its first 4 bytes and its last 8.
for _, width in ipairs({5000, 100000, 2147483647}) do
    local ok, v = pcall(R2SW, 1.5, width, 2)
    out('R2SW 1.5 ' .. width .. ' 2 = ' .. (ok and (#v .. ' "' .. v:sub(1, 4) .. '...' .. v:sub(-8) .. '"') or 'error ' .. tostring(v)))
end
done()
