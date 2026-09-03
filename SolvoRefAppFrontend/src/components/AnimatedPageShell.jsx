import { forwardRef } from 'react';

const AnimatedPageShell = forwardRef(function AnimatedPageShell(
    { className = '', children },
    ref,
) {
    const classes = ['page-shell', className].filter(Boolean).join(' ');

    return (
        <section ref={ref} className={classes}>
            {children}
        </section>
    );
});

export default AnimatedPageShell;
