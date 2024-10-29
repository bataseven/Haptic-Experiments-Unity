clear all
% close all
% clc

saveToTxt = true;
% Set the path to C:\Users\berke\Downloads\MatlabAnalyis\MatlabAnalyis\optimization\
pathToTxt = 'C:\Users\berke\Downloads\MatlabAnalyis\MatlabAnalyis\optimization\';
saveFigure = false;
dataToSave = [];
dataToSaveConflict = [];
ranovaData = [];

%Get the experiment file name
[filenames, pathname] = uigetfile('*.csv', 'Select an experiment file', 'MultiSelect', 'on');

if isequal(filenames, 0) || isequal(pathname, 0)
    disp('File not selected')
    return
else

    if iscell(filenames)
        disp(['Selected ', num2str(length(filenames)), ' files'])

        for i = 1:length(filenames)
            disp(['File ', num2str(i), ': ', filenames{i}])
        end

    else
        disp(['Selected file: ', fullfile(pathname, filenames)])
    end

end

if iscell(filenames)
    numFiles = length(filenames);
else
    numFiles = 1;
end

cumulativeHapticResults = {};
cumulativeVisualResults = {};
cumulativeConflictResults = {};

subjectNames = [];

hapticFileCount = 1;
visualFileCount = 1;

%% Import the file and parse the name
stdAsError = true;

for file = 1:numFiles

    if numFiles == 1
        filename = filenames;
    else
        filename = filenames{file};
    end

    data = readmatrix(fullfile(pathname, filename), 'NumHeaderLines', 0);
    experimentDate = split(filename, '_');
    experimentType = experimentDate{2};
    subjectName = experimentDate{1};
    subjectNames = [subjectNames convertCharsToStrings(subjectName)];
    isVisualConflicted = contains(filename, ["Conflict", "conflict"]);
    % isVisualConflicted = false;

    timeDataAvailable = size(data, 2) >= 4;
    totalTrials = size(data, 1);
    referenceStiffness = mode(data(:, 1:2), 'all');

    adjustedStiffnessValues = [];

    for i = 1:totalTrials

        if data(i, 1) ~= referenceStiffness
            adjustedStiffnessValues = [adjustedStiffnessValues; data(i, 1)];
        else
            adjustedStiffnessValues = [adjustedStiffnessValues; data(i, 2)];
        end

    end

    percentDeltaStiffness = (adjustedStiffnessValues - referenceStiffness) / referenceStiffness * 100;

    uniqueDeltas = unique(percentDeltaStiffness);
    numberOfDeltas = length(uniqueDeltas);
    trialPerCondition = totalTrials / numberOfDeltas;

    correctIndices = [];
    wrongIndices = [];

    % First column is the value of stiffness in percent
    % Second column is the index of correct answers
    % Third column is the index of wrong answers
    % Fourth column is the correct percentage
    % Fifth column is the time it took to answer
    % Sixth column is the average of fifth
    results = num2cell(uniqueDeltas);

    for i = 1:numberOfDeltas
        results{i, 2} = [];
        results{i, 3} = [];
        results{i, 4} = 0;
        results{i, 5} = [];
        results{i, 6} = [];
    end

    for i = 1:totalTrials
        answer = data(i, 3);
        stiffness1 = data(i, 1);
        stiffness2 = data(i, 2);
        percent = percentDeltaStiffness(i);
        deltaIndex = find([results{:, 1}] == percent);

        if timeDataAvailable
            timeItTookToAnswer = data(i, 4);

            if answer ~= -1
                results{deltaIndex, 5} = [results{deltaIndex, 5} timeItTookToAnswer];
            end

            results{deltaIndex, 6} = mean(results{deltaIndex, 5});
        end

        if answer == 0

            if stiffness1 > stiffness2
                results{deltaIndex, 2} = [results{deltaIndex, 2} i];
            else
                results{deltaIndex, 3} = [results{deltaIndex, 3} i];
            end

        elseif answer == 1

            if stiffness2 >= stiffness1
                results{deltaIndex, 2} = [results{deltaIndex, 2} i];
            else
                results{deltaIndex, 3} = [results{deltaIndex, 3} i];
            end

        end

        results{deltaIndex, 4} = length(results{deltaIndex, 2}) / trialPerCondition * 100;
    end

    if experimentType == "HapticOnly"
        cumulativeHapticResults{hapticFileCount} = results;
        hapticFileCount = hapticFileCount + 1;
    else
        cumulativeVisualResults{visualFileCount} = results;
        visualFileCount = visualFileCount + 1;
    end

    % Plot the time it took the answer each stiffness value
    if (timeDataAvailable)
        fprintf("%g) Average Answering Time %-10s %-12s: %.2f seconds\n", file, ['(', subjectName, ')'], ['(', experimentType, ')'], mean([results{:, 6}]))
    end

    % I hate myself for implementing this like this
    if (isVisualConflicted)
        % Count the number of unique lambda values
        uniqueLambdaValues = unique(data(:, 5));
        numberOfLambdas = length(uniqueLambdaValues);
        [UDs, ULs] = meshgrid(uniqueDeltas, uniqueLambdaValues);
        UDULs = cat(2, UDs', ULs');
        possibleCombinations = reshape(UDULs, [], 2);

        % First column is the value of stiffness in percent
        % Second column is the index of correct answers
        % Third column is the index of wrong answers
        % Fourth column is the correct percentage
        % Fifth column is the time it took to answer
        % Sixth column is the average of fifth

        % Create a cell array for each lambda value
        for i = 1:numberOfLambdas
            lambda = uniqueLambdaValues(i);
            eval(['resultsLambda' num2str(lambda * 100) ' = num2cell(uniqueDeltas);']);

            for i = 1:numberOfDeltas
                eval(['resultsLambda' num2str(lambda * 100) '{i, 2} = [];']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 3} = [];']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 4} = 0;']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 5} = [];']);
                eval(['resultsLambda' num2str(lambda * 100) '{i, 6} = [];']);
            end

        end

        for i = 1:totalTrials
            i;
            answer = data(i, 3);
            stiffness1 = data(i, 1);
            stiffness2 = data(i, 2);
            percent = percentDeltaStiffness(i);
            lambda = data(i, 5);
            % deltaIndex = find([eval(['resultsLambda' num2str(lambda * 100) '{:,1}'])] == percent]);
            deltaIndex = eval(['find([resultsLambda' num2str(lambda * 100) '{:,1}] == percent);']);

            if timeDataAvailable
                timeItTookToAnswer = data(i, 4);

                if answer ~= -1
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 5} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 5} timeItTookToAnswer];']);
                end

                eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 6} = mean(resultsLambda' num2str(lambda * 100) '{deltaIndex, 5});']);
            end

            if answer == 0

                if stiffness1 > stiffness2
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} i];']);
                else
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} i];']);
                end

            elseif answer == 1

                if stiffness2 >= stiffness1
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 2} i];']);
                else
                    eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} = [resultsLambda' num2str(lambda * 100) '{deltaIndex, 3} i];']);
                end

            end

            trialCount = length(eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 2}'])) + length(eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 3}']));
            eval(['resultsLambda' num2str(lambda * 100) '{deltaIndex, 4} = length(resultsLambda' num2str(lambda * 100) '{deltaIndex, 2}) / trialCount * 100;']);
        end

        % Plot the correct percentage of lambda values on the same bar graph
        % figure
        x = uniqueLambdaValues * 100;
        y = [];

        for i = 1:numberOfLambdas
            lambda = uniqueLambdaValues(i);
            lambdaResults = [];

            for j = 1:numberOfDeltas
                lambdaResults = [lambdaResults eval(['resultsLambda' num2str(lambda * 100) '{j, 4}'])];
            end

            y = [y; lambdaResults];
        end

        cumulativeConflictResults{file} = y;

        resultsLambda = [resultsLambda0; resultsLambda25; resultsLambda50; resultsLambda75; resultsLambda100];
        trialResults = [];

        for t = 1:10 % Trial
            conditionResults = [];

            for c = 1:20 % condition
                delta = possibleCombinations(c, 1);
                lambda = possibleCombinations(c, 2);
                idxOfLambda = find(uniqueLambdaValues == lambda);
                idxOfDelta = find(uniqueDeltas == delta);
                rowToSearch = resultsLambda((idxOfLambda - 1) * numberOfDeltas + idxOfDelta, :);

                if isempty(rowToSearch{1, 2})
                    conditionResults = [conditionResults 0];
                    resultsLambda{(idxOfLambda - 1) * numberOfDeltas + idxOfDelta, 3}(1) = [];
                elseif isempty(rowToSearch{1, 3})
                    conditionResults = [conditionResults 1];
                    resultsLambda{(idxOfLambda - 1) * numberOfDeltas + idxOfDelta, 2}(1) = [];
                else

                    if (length(rowToSearch{1, 2}(1)) < length(rowToSearch{1, 3}(1)))
                        conditionResults = [conditionResults 1];
                        resultsLambda{(idxOfLambda - 1) * numberOfDeltas + idxOfDelta, 2}(1) = [];
                    else
                        conditionResults = [conditionResults 0];
                        resultsLambda{(idxOfLambda - 1) * numberOfDeltas + idxOfDelta, 3}(1) = [];
                    end

                end

            end

            trialResults = [trialResults; [t conditionResults]];
        end

        ranovaData = [ranovaData; [ones(10, 1) * file trialResults]];
    end

end

% PLot the cumulative results
if (~isVisualConflicted)
    % Calculate the mean and standard deviation of the cumulative results
    hapticCumulativePercentCorrect = [];

    for i = 1:length(cumulativeHapticResults)
        hapticCumulativePercentCorrect = [hapticCumulativePercentCorrect; [cumulativeHapticResults{i}{:, 4}]];
    end

    hapticCumulativeStandardDeviation = std(hapticCumulativePercentCorrect, 0, 1);
    hapticCumulativeMean = mean(hapticCumulativePercentCorrect, 1);

    visualCumulativePercentCorrect = [];

    for i = 1:length(cumulativeVisualResults)
        visualCumulativePercentCorrect = [visualCumulativePercentCorrect; [cumulativeVisualResults{i}{:, 4}]];
    end

    visualCumulativeStandardDeviation = std(visualCumulativePercentCorrect, 0, 1);
    visualCumulativeMean = mean(visualCumulativePercentCorrect, 1);

    % Plot the haptic and visual results as seperate bars
    figure
    ax = gca;
    ax.FontSize = 16;
    ax.FontWeight = 'bold';

    b = bar([results{:, 1}], [hapticCumulativeMean; visualCumulativeMean], 'grouped');

    ngroups = 7;

    model_error = [];

    if isempty(hapticCumulativeMean)
        legend('Visual+Haptic')

        if stdAsError
            model_error = visualCumulativeStandardDeviation;
        end

        model_series = visualCumulativeMean;
        nbars = 1;
    elseif isempty(visualCumulativeMean)
        legend('Haptic Only')

        if stdAsError
            model_error = hapticCumulativeStandardDeviation;
        end

        model_series = hapticCumulativeMean;
        nbars = 1;
    else
        legend('Haptic Only', 'Visual+Haptic')

        if stdAsError
            model_error = [hapticCumulativeStandardDeviation; visualCumulativeStandardDeviation];
        end

        model_series = [hapticCumulativeMean; visualCumulativeMean];
        nbars = 2;
    end

    xCoords = nan(nbars, ngroups);

    for i = 1:nbars
        xCoords(i, :) = b(i).XEndPoints;
    end

    hold on
    errorbar(xCoords, model_series, model_error, 'k', 'linestyle', 'none', 'HandleVisibility', 'off');

    % errorbar([results{:, 1}], hapticCumulativeMean, hapticCumulativeStandardDeviation, 'LineStyle', 'none', 'Color', 'black')
    % errorbar([results{:, 1}], visualCumulativeMean, visualCumulativeStandardDeviation, 'LineStyle', 'none', 'Color', 'black')
    ylim([0 100])
    xlim([min([results{:, 1}]) - 5, max([results{:, 1}]) + 5])
    ylabel('Percent Correct (%)')
    xlabel('Percent Change in Stiffness (%)')

    if false && (length(subjectNames) == 1 || ~any(~strcmp(subjectName, subjectNames), 'all'))
        title("Result of " + subjectNames(1))
    else
        title("Result of " + num2str(length(unique(subjectNames))) + " Subjects")
    end

else
    conflictCumulativeMean = sum(cat(3, cumulativeConflictResults{:}), 3) / length(cumulativeConflictResults);

    myFig = figure;
    b = bar(x, conflictCumulativeMean);
    
    % Set the size of the figure to be the same as the size of the screen
    myFig.Position = [0 0 1.0592e+03 * 0.8 732.8000 * 0.8];


    ax = gca;
    ax.FontSize = 16;
    ax.FontWeight = 'bold';
    ax.FontName = 'Times New Roman';

    if false && (length(subjectNames) == 1 || ~any(~strcmp(subjectName, subjectNames), 'all'))
        title("Conflict Result of " + subjectNames)
    else
        title("Conflict Result of " + num2str(length(unique(subjectNames))) + " Subjects")
    end
    ttl = title("a)", "FontSize", 26, "FontWeight", "normal");

    legend( {'$$\Delta{K} = 0.25K_{0}$$', ...
                    '$$\Delta{K} = 0.50K_{0}$$', ...
                        '$$\Delta{K} = 0.75K_{0}$$', ...
                        '$$\Delta{K} = 1.00K_{0}$$', ...
                    }, ...
                    'Location', 'northeast', 'Interpreter', 'latex', 'FontSize', 18)
    xlabel('Percent Conflict (\lambda*100%)', 'FontSize', 16)
    ylabel([{'Variable Object Perceived Stiffer than Reference (%)'}], 'FontSize', 16)

    ylim([0 100])

    model_series = conflictCumulativeMean;
    model_error = std(cat(3, cumulativeConflictResults{:}), 0, 3);

    nbars = 4;
    ngroups = 5;
    xCoords = nan(nbars, ngroups);

    for i = 1:nbars
        xCoords(i, :) = b(i).XEndPoints;
    end

    hold on
    errorbar(xCoords', model_series, model_error, 'k', 'linestyle', 'none', 'HandleVisibility', 'off');

    if (saveFigure)
        saveas(myFig, [num2str(length(subjectNames)) 'Subjects' '.fig'])
        saveas(myFig, [num2str(length(subjectNames)) 'Subjects' '.png'])
    end

    axis equal

end

% Save the data in to a txt file where the first column is the haptic+visual and the second column is the haptic only
if (~isVisualConflicted && saveToTxt && length(cumulativeHapticResults) ~= 0 && (length(cumulativeVisualResults) == length(cumulativeHapticResults)))

    singleExperimentData = [];

    for (i = 1:7)
        correctPercentages = [];

        for (j = 1:length(cumulativeVisualResults))

            if (i < 4)
                correctPercentages = [correctPercentages; 100 - [cumulativeVisualResults{j}{i, 4}]];
            else
                correctPercentages = [correctPercentages; [cumulativeVisualResults{j}{i, 4}]];
            end

        end

        singleExperimentData = [singleExperimentData; correctPercentages];
    end

    dataToSave = singleExperimentData;
    singleExperimentData = [];

    for (i = 1:7)
        correctPercentages = [];

        for (j = 1:length(cumulativeHapticResults))

            if (i < 4)
                correctPercentages = [correctPercentages; 100 - [cumulativeHapticResults{j}{i, 4}]];
            else
                correctPercentages = [correctPercentages; [cumulativeHapticResults{j}{i, 4}]];
            end

        end

        singleExperimentData = [singleExperimentData; correctPercentages];
    end

    dataToSave = [dataToSave singleExperimentData];
    writematrix(dataToSave, 'stiff_discrimination_modified.txt', 'Delimiter', 'tab')

elseif isVisualConflicted && saveToTxt

    for i = 1:5
        singleLineOfData = [];

        for (j = 1:length(cumulativeConflictResults))
            singleLineOfData = [singleLineOfData; cumulativeConflictResults{j}(i, :)];
        end

        dataToSaveConflict = [dataToSaveConflict; singleLineOfData];

    end

    % fileID = fopen('exp.txt','w');
    % fprintf(fileID,'%6s %12s\n','x','exp(x)');
    % fclose()

    disp('Saving to txt file');
    % Print the data to a txt file
    writematrix(dataToSaveConflict, 'ConflictExp.txt', 'Delimiter', 'space')
    path = convertCharsToStrings(pathToTxt) + "ConflictExp.txt";
    writematrix(dataToSaveConflict, path, 'Delimiter', 'space')
else
    disp('Data not saved')
end

disp('Done')
