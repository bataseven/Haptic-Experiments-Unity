% clear all
close all
% clc

N = 3;

% Get the experiment file name
[filename, pathname] = uigetfile('*.csv', 'Select an experiment file');
if isequal(filename,0) || isequal(pathname,0)
    disp('File not selected')
    return
else
    disp(['Selected file: ', fullfile(pathname, filename)])
end

% Import the file
data = readmatrix(fullfile(pathname, filename),'NumHeaderLines',0);

experimentDate = split(filename, '_');
subjectName = experimentDate{1};
referenceStiffness = mode(data(:,1:2),'all');



trialCount = size(data,1);
reversalCount = 0;
fprintf("Trial Count: %g\n", trialCount);

isCorrectPrevious = true;

correctChoices = [];
incorrectChoices = [];

reversalValueAverages = [];
reversalValues = [];

for i = 1 : trialCount
    [adjustedStiffness, idx] = max(data(i,1:2));
    isCorrect = idx == data(i,3);

    if isCorrect
        correctChoices = [correctChoices; [i adjustedStiffness]];
    else
        incorrectChoices = [incorrectChoices; [i adjustedStiffness]];
    end
    if isCorrect ~= isCorrectPrevious
        if(isCorrect)
            if size(correctChoices, 1) > 1
                reversalValueAverages = [reversalValueAverages (correctChoices(end, 2) + correctChoices(end - 1, 2))/2];
            else
                reversalValueAverages = [reversalValueAverages correctChoices(end, 2)];
            end
            % Find the element in data(i, 1:2) that is not equal to 1
            reversalValue = data(i, 1);
            if reversalValue == 1
                reversalValue = data(i, 2);
            end
            reversalValues = [reversalValues reversalValue];
        end
        reversalCount = reversalCount + 1;
    end
    isCorrectPrevious = isCorrect;
end

% Create and adjust the figure
figure
hold on
grid on
grid minor

ylabel('Stiffness [N/mm]')
xlabel('Trials')
title({['Subject: ' subjectName ],['Reference Stiffness: ' num2str(referenceStiffness) ' [N/mm]']})

ylim([0 13])
xlim([0 trialCount])

% Variables to hold the axes and their legend names
plots = [];
legends = {};

% Plot adjusted stiffness values
if ~isempty(correctChoices)
    plt = plot(correctChoices(:,1), correctChoices(:,2), 'go', 'MarkerSize', 8, 'LineWidth',2);
    plots = [plots plt];
    legends = [legends 'Correct'];
end
if ~isempty(incorrectChoices)
    plt = plot(incorrectChoices(:,1), incorrectChoices(:,2), 'rx', 'MarkerSize', 8, 'LineWidth',2);
    plots = [plots plt];
    legends = [legends 'Incorrect'];
end

% Draw reference line
plt = plot([0 size(data,1)+1], [referenceStiffness referenceStiffness], 'k--');
plots = [plots plt];
legends = [legends 'Reference'];

% Put the legends
legend(plots, legends)

% Display the reversal count as a text
% text(0.01, 0.025, ['Reversal Count: ' num2str(reversalCount)], 'Units', 'normalized');


% if length(reversalValueAverages) > N - 1
%     nLastAverages = mean(reversalValueAverages(end - N + 1 : end));
%     fprintf("Average of the %d last reversals is %.2f", N, nLastAverages)
%     text(0.01, 0.045, ['Average of last ' num2str(N) ' reversals: ' num2str(nLastAverages, 3) ' N/mm'], 'Units', 'normalized');
% else
%     fprintf('Not enough reversals to calculate the average of last %d reversals\n', N * 2);
% end

if length(reversalValues) > N - 1
    nLastAverages = mean(reversalValues(end - N + 1 : end));
    fprintf("Average of the %d last reversals is %.2f", N, nLastAverages)
    text(0.01, 0.045, ['Average of last ' num2str(N) ' reversals: ' num2str(nLastAverages, 3) ' N/mm'], 'Units', 'normalized');
else
    fprintf('Not enough reversals to calculate the average of last %d reversals\n', N * 2);
end




