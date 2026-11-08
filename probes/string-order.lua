-- The order pairs visits string keys of several lengths in, for the game's string hash seed (decisions D35, D126,
-- W1-10): lua-config's 20 short keys fix only the seed's low 20 bits, as a key's hash reaches its main position
-- through two bits a letter. Each line: the key length, then the keys' numbers (1 to 24, set in that order) in the
-- order pairs visits them. Key n is "x" up to the length, ending in the n-th letter of the alphabet, which Lua hashes
-- first.
for _, length in ipairs({8, 14, 22, 30, 38, 60}) do
    local t, order = {}, {}
    for n = 1, 24 do
        t[string.rep('x', length - 1) .. string.char(96 + n)] = n
    end
    for _, n in pairs(t) do order[#order + 1] = n end
    out('order ' .. length .. ' ' .. table.concat(order, ' '))
end
done()
