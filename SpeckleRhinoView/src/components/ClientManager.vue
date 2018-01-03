<template>
  <div >
    <receiver-add></receiver-add>
    <sender-add></sender-add>
    <v-container fluid v-if='clients.length > 0' style='min-height: 100%;' class='pa-0'>
      <template v-for='client in clients'>
        <receiver v-if='client.Role === 1' :client='client'></receiver>
        <sender v-else :client='client'></sender>
      </template>
    </v-container>
  </div>
</template>
<script>
import ReceiverAdd from './ClientReceiverAdd.vue'
import SenderAdd from './ClientSenderAdd.vue'
import Receiver from './Receiver.vue'
import Sender from './Sender.vue'
import { EventBus } from '../event-bus'

export default {
  name: 'AccountsManager',
  components: {
    ReceiverAdd,
    SenderAdd,
    Receiver,
    Sender
  },
  computed: {
    clients( ) {
      return this.$store.getters.clients
    }
  },
  data( ) {
    return {
      addClientDialog: false
    }
  },
  methods: {
    getFileClients( ) {}
  },
  mounted( ) {}
}
</script>
<style scoped>
.receiver:last-child {
  margin-bottom: 100px;
}
</style>