clear
clc
%% Stiffness and Lambda values to be paired up
stiffnessValues1 = [-30 -20 -10 0 10 20 30]; % dk / k_0   =>  Delta stiffness over reference stiffness percent
stiffnessValues2 = [25 50 75 100]; % dk / k_0   =>  Delta stiffness over reference stiffness percent
lambdaValues = [0 0.25 0.5 0.75 1];
trialCount1 = 10;
trialCount2 = 5;

%% Generate first stiffness values for the non-conflict experiment (Haptic Only)
rng(1)
duplicatedStiffnessValues = repmat(stiffnessValues1, 1, trialCount1);

% Array of stiffness values to be paired with the reference stiffness
stiffnessPairs1 =  duplicatedStiffnessValues(randperm(length(duplicatedStiffnessValues)))';

% Print on command window to copy and paste
fprintf('\n{');
for i = 1 : length(stiffnessPairs1)
fprintf('%gf',stiffnessPairs1(i))
if i ~= length(stiffnessPairs1)
    fprintf(',');
end
end
fprintf('};\n');
%% Generate second stiffness values for the non-conflict experiment (Haptic + Visual)
rng(2)
duplicatedStiffnessValues = repmat(stiffnessValues1, 1, trialCount1);

% Array of stiffness values to be paired with the reference stiffness
stiffnessPairs2 =  duplicatedStiffnessValues(randperm(length(duplicatedStiffnessValues)))';

% Print on command window to copy and paste
fprintf('\n{');
for i = 1 : length(stiffnessPairs2)
fprintf('%gf',stiffnessPairs2(i))
if i ~= length(stiffnessPairs2)
    fprintf(',');
end
end
fprintf('};\n');
%% Generate stiffness & lambda pairs for the conflict experiments
rng(1)
% duplicatedStiffnessValues = repmat(, 1, trialCount2);
% duplicatedLambdaValues = repmat(, 1, trialCount2);

[SV, LV] = meshgrid(stiffnessValues2, lambdaValues);
SVLV = cat(2,SV',LV');

pairs = reshape(SVLV, [], 2);
multipePairs = repmat(pairs, trialCount2, 1);
randomizedMultiplePairs = multipePairs(randperm(size(multipePairs, 1)), :);

fprintf('\n{{');
for i = 1 : length(randomizedMultiplePairs)
fprintf('%gf',randomizedMultiplePairs(i,1))
if i ~= length(randomizedMultiplePairs)
    fprintf(',');
end
end
fprintf('},\n{');
for i = 1 : length(randomizedMultiplePairs)
fprintf('%gf',randomizedMultiplePairs(i,2))
if i ~= length(randomizedMultiplePairs)
    fprintf(',');
end
end
fprintf('}};\n');