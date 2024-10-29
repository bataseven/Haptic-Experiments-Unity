% Define the rotated paraboloid equation
% Define the rotated paraboloid equation
eqn = fittype(@(x,y,a,b,c,theta) ((a*c*sin(theta)^2 - b*c*cos(theta)^2)*x.^2 + (a*sin(theta)^2 + b*cos(theta)^2)*y.^2 + 2*y.*x*c*sin(2*theta))/((c^2)*sin(theta)^2 + cos(theta)^2) + ((c*x.^2 + b*y.^2 + a*(x.^2+y.^2))/c)*cos(theta));

% Generate sample data
x = -5:0.1:5;
y = -5:0.1:5;
[X,Y] = meshgrid(x,y);
a = 1;
b = 2;
c = 3;
theta = pi/4;
Z = ((a*c*sin(theta)^2 - b*c*cos(theta)^2)*X.^2 + (a*sin(theta)^2 + b*cos(theta)^2)*Y.^2 + 2*Y.*X*c*sin(2*theta))/((c^2)*sin(theta)^2 + cos(theta)^2) + ((c*X.^2 + b*Y.^2 + a*(X.^2+Y.^2))/c)*cos(theta);
Z = Z(:); % Reshape Z into a column vector
data = [X(:), Y(:), Z]; % Combine X, Y, and Z into a matrix

% Fit the data to the rotated paraboloid equation
startPoint = [1 1 1 theta]; % Initial guess for the equation parameters
fitResult = fit(data(:,1:2), data(:,3), eqn, 'Start', startPoint);

% Display the coefficients
coeffs = coeffvalues(fitResult)
