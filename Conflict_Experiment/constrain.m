function y = constrain(x,lower,upper)
    y=min(max(x,lower),upper);