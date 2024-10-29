clear
clc
%% Stiffness and Lambda values to be paired up
stiffnessValues1 = [-30 -20 -10 0 10 20 30]; % dk / k_0   =>  Delta stiffness over reference stiffness percent
stiffnessValues2 = [25 50 75 100]; % dk / k_0   =>  Delta stiffness over reference stiffness percent
lambdaValues = [0 0.25 0.5 0.75 1];
lambdaValues = [1]
groupAs = 7;
trialCount1 = 10;
trialCount2 = 5;
%% Generate randomized reference object order
rng(1)
referenceValues = repmat([0 1], 1, trialCount1 * length(stiffnessValues1) / 2);
randomizedReferenceValues = referenceValues(randperm(length(referenceValues)))';
% Print on command window to copy and paste
fprintf('\n{');

for i = 1:length(randomizedReferenceValues)
    fprintf('%g', randomizedReferenceValues(i))

    if i ~= length(randomizedReferenceValues)
        fprintf(',');
    end

end

fprintf('};\n');


%% Generate first stiffness values for the non-conflict experiment (Haptic Only)
rng(1)
duplicatedStiffnessValues = repmat(stiffnessValues1, 1, trialCount1);

% Array of stiffness values to be paired with the reference stiffness
stiffnessPairs1 = [];

for i = 1:trialCount1
    pairsToConcat = stiffnessValues1(randperm(groupAs))';
    while i ~= 1 && pairsToConcat(1) == stiffnessPairs1(end)
        pairsToConcat = stiffnessValues1(randperm(groupAs))';
    end
    stiffnessPairs1 = [stiffnessPairs1; pairsToConcat];
end

% Print on command window to copy and paste
fprintf('\n{');

for i = 1:length(stiffnessPairs1)
    fprintf('%gf', stiffnessPairs1(i))

    if i ~= length(stiffnessPairs1)
        fprintf(',');
    end

end

fprintf('};\n');

noOverlapping = true;
% Validation
for i = 1:length(stiffnessPairs1)
    if i == 1
        continue
    end
    if stiffnessPairs1(i) == stiffnessPairs1(i - 1)
        noOverlapping = false;
        fprintf('Overlapping at index %d, value %f)', i, stiffnessPairs1(i));
        break;
    end
end

%% Generate second stiffness values for the non-conflict experiment (Haptic + Visual)
rng(2)
duplicatedStiffnessValues = repmat(stiffnessValues1, 1, trialCount1);

% Array of stiffness values to be paired with the reference stiffness
stiffnessPairs2 = duplicatedStiffnessValues(randperm(length(duplicatedStiffnessValues)))';

% Print on command window to copy and paste
fprintf('\n{');

for i = 1:length(stiffnessPairs2)
    fprintf('%gf', stiffnessPairs2(i))

    if i ~= length(stiffnessPairs2)
        fprintf(',');
    end

end

fprintf('};\n');
%% Generate stiffness & lambda pairs for the conflict experiments
rng(3)
% duplicatedStiffnessValues = repmat(, 1, trialCount2);
% duplicatedLambdaValues = repmat(, 1, trialCount2);

[SV, LV] = meshgrid(stiffnessValues2, lambdaValues);
SVLV = cat(2, SV', LV');

pairs = reshape(SVLV, [], 2);
multipePairs = repmat(pairs, trialCount2, 1);
randomizedMultiplePairs = multipePairs(randperm(size(multipePairs, 1)), :);
ctr = 1;
while true
    ctr = ctr+1;
    consecutiveSameRows = false;
    for i = 1 : length(randomizedMultiplePairs) - 1
        if randomizedMultiplePairs(i, :) == randomizedMultiplePairs(i + 1, :)
            consecutiveSameRows = true;
            break
        end
    end
    if ~consecutiveSameRows
        break
    end
    randomizedMultiplePairs = multipePairs(randperm(size(multipePairs, 1)), :);
end

% randomizedMultiplePairs = randomizedMultiplePairs(101:end,:);

% multipePairs = sortrows(multipePairs, 2);
% randomizedMultiplePairs= multipePairs;

fprintf('\n{{');

for i = 1:length(randomizedMultiplePairs)
    fprintf('%gf', randomizedMultiplePairs(i, 1))

    if i ~= length(randomizedMultiplePairs)
        fprintf(',');
    end

end

fprintf('},\n{');

for i = 1:length(randomizedMultiplePairs)
    fprintf('%gf', randomizedMultiplePairs(i, 2))

    if i ~= length(randomizedMultiplePairs)
        fprintf(',');
    end

end

fprintf('}};\n');
