const mongoose = require('mongoose');

const adminLogsSchema = new mongoose.Schema({
    admin_id: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User', 
        required: true
    },
    action: {
        type: String,
        required: true
    },
    target_type: {
        type: String,
        enum: ['user', 'recipe'], 
        required: true
    },
    target_id: {
        type: mongoose.Schema.Types.ObjectId, 
        required: true
    },
    action_date: {
        type: Date,
        default: Date.now
    }
});

module.exports = mongoose.model('AdminLogs', adminLogsSchema);