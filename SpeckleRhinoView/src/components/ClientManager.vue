<template>
  <v-container fluid pa-0>
    <receiver-add></receiver-add>
    <sender-add></sender-add>
    <v-layout wrap column>
      <v-flex xs12 v-for='client in clients' :key='client.restApi' flat class='receiver'>
        <receiver v-if='client.Role === 1' :client='client'></receiver>
        <sender v-else :client='client'></sender>
      </v-flex>
    </v-layout>
  </v-container>
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
    clients() { 
      return this.$store.getters.clients
    }
  },
  data() {
    return {
      addClientDialog: false
    }
  },
  methods: {
    getFileClients() {
    }
  },
  mounted () {
  }
}
</script>

<style scoped>
  .receiver:last-child{
    /*margin-bottom: 100px;*/
  }
</style>