syms x y z dx dy dz v

% eq = x * y * z == (v * z * dx / dz + dx) * (y + dy) * (z + dz);
% eq = x * y * z == (x + dx) * (y + dy) * (dz * x / v / dx + dz);
eq = x * y * z == (x + dx) * (y + dy) * (z + v * z * dx / x);

sol = solve(eq, dy, "Real",true,"ReturnConditions",true)


