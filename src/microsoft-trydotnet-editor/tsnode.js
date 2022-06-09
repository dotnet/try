const tsAutoMockTransformer = require('ts-auto-mock/transformer').default;
require('ts-node').register({
    transformers: program => ({
        before: [
            tsAutoMockTransformer(program)
        ]
    })
});