<template>
  <div>
    <receiver-add></receiver-add>
    <sender-add></sender-add>
    <v-slide-y-transition>
      <v-card fluid fill-height class='pa-4 elevation-0' v-show='glLoading'>
        Loading...
        <v-progress-linear height='1' :indeterminate='true'></v-progress-linear>
      </v-card>
    </v-slide-y-transition>
    <v-card fluid fill-height v-if='clients.length == 0 && !glLoading' class='elevation-0 pa-4'>
      <h4>Hey there!</h4>
      <p>There are no clients in this file. You can add some from the big button in the lower right corner!</p>
    </v-card>
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
    },
    glLoading( ) {
      return this.$store.state.globalLoading
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